using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
// Disambiguates from UnityEngine.TerrainData, which "using UnityEngine" also brings in.
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.LandmarksModule
{
	/// <summary>
	/// Sits landmark buildings on elevated terrain by driving their transforms, and keeps them there
	/// as the map moves and terrain arrives.
	///
	/// Owned by <see cref="LandmarksLayerModule"/> and constructed when the module is attached to a
	/// visualizer, mirroring how <c>TerrainLayerModule</c> owns its bounds tracker. The visualizer
	/// does not know this exists.
	///
	/// Why a transform and not the mesh: elevation is not necessarily known when a landmark is
	/// generated - the terrain tile under it may still be in flight, and in shader mode the CPU
	/// decode lands after the tile is already "loaded". Baking a height into vertices at generation
	/// time is therefore a one-way bet, and losing it is permanent: that is exactly what happens to
	/// vector features, where SnapTerrainModifier writes elevation into MeshData and nothing ever
	/// re-snaps, so a building that generated a frame too early stays at sea level on a hillside for
	/// the rest of the session. Landmarks are positioned by transform, so a late correction costs
	/// one assignment.
	///
	/// Elevation is cached in METRES and the Unity Y derived on demand, because the two things that
	/// invalidate it are not equally expensive. A change to <see cref="IMapInformation.Scale"/> -
	/// which happens on every zoom step under DynamicScalingMapInformation - only changes the
	/// divisor, so it needs a re-derivation and no terrain sampling at all. Only terrain actually
	/// arriving requires a re-query, and then only for buildings still lacking a reading.
	/// </summary>
	internal sealed class LandmarkTerrainSnapper : IDisposable
	{
		private readonly IMapInformation _mapInformation;

		// Anchors per landmark data tile, so a tile's visuals can be dropped in one step when its
		// data is disposed.
		private readonly Dictionary<CanonicalTileId, List<Entry>> _entriesByTile =
			new Dictionary<CanonicalTileId, List<Entry>>();

		// One-shot watches for terrain data whose CPU decode has not landed yet. Held as
		// (data, handler) so Dispose can detach the closures, and so the same shared data tile is
		// not watched twice - one terrain data tile is sampled by up to 16 render tiles.
		private readonly List<(TerrainData data, Action handler)> _pendingWatches =
			new List<(TerrainData, Action)>();

		// Terrain data tiles whose elevation has just become available, so a drain only revisits the
		// landmark tiles those actually cover instead of re-walking every registered tile.
		private readonly HashSet<CanonicalTileId> _terrainArrivals = new HashSet<CanonicalTileId>();

		private ITileLifecycleSource _source;
		private Coroutine _drainCoroutine;
		private bool _dirty;
		private bool _disposed;

		public LandmarkTerrainSnapper(ITileLifecycleSource source, IMapInformation mapInformation)
		{
			_source = source;
			_mapInformation = mapInformation;

			_source.TileLoaded += OnTileLoaded;

			if (Runnable.Instance != null)
			{
				_drainCoroutine = Runnable.Instance.StartCoroutine(DrainLoop());
			}
			else
			{
				// Runnable is a global singleton, so this should not happen in play. Fail loud
				// rather than silently never snapping anything.
				Debug.LogWarning($"{nameof(LandmarkTerrainSnapper)}: Runnable.Instance is null at construction; landmarks will not be snapped to terrain.");
			}
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			if (_source != null)
			{
				_source.TileLoaded -= OnTileLoaded;
				_source = null;
			}

			for (var i = 0; i < _pendingWatches.Count; i++)
			{
				var (data, handler) = _pendingWatches[i];
				if (data != null && handler != null)
				{
					data.ElevationValuesUpdated -= handler;
				}
			}
			_pendingWatches.Clear();
			_entriesByTile.Clear();
			_terrainArrivals.Clear();

			if (_drainCoroutine != null && Runnable.Instance != null)
			{
				Runnable.Instance.StopCoroutine(_drainCoroutine);
				_drainCoroutine = null;
			}
		}

		/// <summary>
		/// Takes ownership of a freshly generated tile's buildings and snaps whatever can be snapped
		/// straight away. Buildings whose terrain is not loaded yet stay at ground level and are
		/// picked up by a later pass.
		/// </summary>
		public void Register(CanonicalTileId tileId, List<LandmarkAnchor> anchors)
		{
			if (_disposed || anchors == null || anchors.Count == 0) return;

			if (!_entriesByTile.TryGetValue(tileId, out var entries))
			{
				entries = new List<Entry>(anchors.Count);
				_entriesByTile[tileId] = entries;
			}

			for (var i = 0; i < anchors.Count; i++)
			{
				var anchor = anchors[i];
				if (anchor.Transform == null) continue;
				entries.Add(new Entry(anchor));
			}

			ResolveTile(tileId, entries);
		}

		/// <summary>Drops a tile's buildings, called when its data is disposed.</summary>
		public void Unregister(CanonicalTileId tileId)
		{
			_entriesByTile.Remove(tileId);
		}

		/// <summary>
		/// Re-derives every known Y from the cached metres. Call when the map's scale or centre
		/// changes - the stored elevation is still valid, only its conversion to Unity units moved.
		/// Deliberately does not sample terrain, so this stays cheap enough for every zoom step.
		/// </summary>
		public void ReapplyForViewChange()
		{
			if (_disposed) return;

			foreach (var pair in _entriesByTile)
			{
				var entries = pair.Value;
				for (var i = 0; i < entries.Count; i++)
				{
					entries[i].Apply(_mapInformation.Scale);
				}
			}
		}

		private IEnumerator DrainLoop()
		{
			while (!_disposed)
			{
				if (_dirty)
				{
					_dirty = false;
					ResolveArrivals();
				}

				yield return null;
			}
		}

		private void OnTileLoaded(UnityMapTile tile)
		{
			if (_disposed) return;

			// No terrain data means no terrain module in the scene, or none for this tile - either
			// way there is nothing to snap to and nothing to wait for. This is also what keeps a
			// flat-map scene from doing any work at all: without it, every tile load would queue a
			// pass that could never succeed.
			var terrainData = tile.TerrainContainer?.TerrainData;
			if (terrainData == null) return;

			if (terrainData.IsElevationDataReady)
			{
				_terrainArrivals.Add(terrainData.TileId);
				_dirty = true;
			}
			else
			{
				// In shader mode a tile finishes on texture-ready, before the elevation array has
				// been decoded, so the query would still fail. Wait for the decode instead.
				WatchForAsyncDecode(terrainData);
			}
		}

		private void WatchForAsyncDecode(TerrainData data)
		{
			for (var i = 0; i < _pendingWatches.Count; i++)
			{
				if (ReferenceEquals(_pendingWatches[i].data, data)) return;
			}

			Action handler = null;
			Action onDispose = null;

			handler = () =>
			{
				data.ElevationValuesUpdated -= handler;
				data.RemoveDisposeCallback(onDispose);
				RemoveWatch(data, handler);
				if (_disposed) return;

				_terrainArrivals.Add(data.TileId);
				_dirty = true;
			};

			// Dispose does not raise ElevationValuesUpdated, so without this the entry would keep
			// both the data and the closure alive until our own Dispose. Eviction mid-decode is the
			// trigger.
			onDispose = () =>
			{
				data.ElevationValuesUpdated -= handler;
				data.RemoveDisposeCallback(onDispose);
				RemoveWatch(data, handler);
			};

			_pendingWatches.Add((data, handler));
			data.ElevationValuesUpdated += handler;
			data.AddDisposeCallback(onDispose);
		}

		private void RemoveWatch(TerrainData data, Action handler)
		{
			for (var i = _pendingWatches.Count - 1; i >= 0; i--)
			{
				if (ReferenceEquals(_pendingWatches[i].data, data) &&
				    ReferenceEquals(_pendingWatches[i].handler, handler))
				{
					_pendingWatches.RemoveAt(i);
					break;
				}
			}
		}

		/// <summary>
		/// Re-samples only the landmark tiles the newly decoded terrain actually covers.
		///
		/// One terrain data tile is shared by up to 16 render tiles, so a single decode arrives as a
		/// burst of TileLoaded events - which is what the dirty flag coalesces. Without also
		/// narrowing by coverage, each burst re-walked every registered landmark tile and every
		/// footprint vertex in it, almost all of them for terrain that had not changed.
		/// </summary>
		private void ResolveArrivals()
		{
			foreach (var pair in _entriesByTile)
			{
				if (!IsCoveredByArrival(pair.Key)) continue;
				ResolveTile(pair.Key, pair.Value);
			}

			_terrainArrivals.Clear();
		}

		/// <summary>
		/// Whether any just-arrived terrain tile is one an elevation query for this landmark tile
		/// would actually consult - that is, the landmark tile itself or one of its ancestors, since
		/// TryQueryElevation walks upward from the id it is given.
		/// </summary>
		private bool IsCoveredByArrival(CanonicalTileId landmarkTileId)
		{
			foreach (var terrainTileId in _terrainArrivals)
			{
				if (terrainTileId.Z > landmarkTileId.Z) continue;

				if (landmarkTileId.ParentAt(terrainTileId.Z).Equals(terrainTileId)) return true;
			}

			return false;
		}

		/// <summary>
		/// Samples terrain for every building in a tile that does not have a reading yet, and
		/// applies it. Buildings that already have one are left alone - elevation for a given point
		/// does not change, so re-sampling would only cost time.
		/// </summary>
		private void ResolveTile(CanonicalTileId tileId, List<Entry> entries)
		{
			var scale = _mapInformation.Scale;

			for (var i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				if (entry.HasElevation) continue;

				if (TrySampleGroundHeight(tileId, entry, out var metres))
				{
					entry.SetElevation(metres, scale);
				}
			}
		}

		/// <summary>
		/// Lowest terrain height under a building, in metres.
		///
		/// The minimum over the footprint rather than the height at the centre, because a building
		/// has a flat base: snapping to the centre height leaves the downhill edge floating in the
		/// air on any slope, whereas snapping to the lowest point buries the uphill edge, which is
		/// what a real foundation does and reads as correct. Falls back to the anchor point when the
		/// tile published no footprint for this landmark.
		///
		/// Returns false when no elevation data covers the point, so the caller can leave the
		/// building at ground level and try again later rather than committing a placeholder 0 -
		/// which is indistinguishable from genuine sea level once written.
		/// </summary>
		private bool TrySampleGroundHeight(CanonicalTileId tileId, Entry entry, out float metres)
		{
			metres = 0f;

			var footprint = entry.FootprintUvs;
			if (footprint == null || footprint.Length == 0)
			{
				return _mapInformation.TryQueryElevation(tileId, entry.GroundUv.x, entry.GroundUv.y, out metres);
			}

			var found = false;
			for (var i = 0; i < footprint.Length; i++)
			{
				if (!_mapInformation.TryQueryElevation(tileId, footprint[i].x, footprint[i].y, out var sample))
				{
					continue;
				}

				if (!found || sample < metres)
				{
					metres = sample;
					found = true;
				}
			}

			// A footprint that straddles a tile edge has vertices outside 0-1; those samples are
			// clamped to the edge rather than dropped, so a partial result is still usable.
			if (!found)
			{
				return _mapInformation.TryQueryElevation(tileId, entry.GroundUv.x, entry.GroundUv.y, out metres);
			}

			return true;
		}

		/// <summary>
		/// One building's snapping state. Holds elevation in metres rather than in Unity units so a
		/// scale change is a re-derivation instead of a re-sample.
		/// </summary>
		private sealed class Entry
		{
			public readonly Vector2 GroundUv;
			public readonly Vector2[] FootprintUvs;
			public bool HasElevation { get; private set; }

			private readonly Transform _transform;
			private float _metres;

			public Entry(LandmarkAnchor anchor)
			{
				_transform = anchor.Transform;
				GroundUv = anchor.GroundUv;
				FootprintUvs = anchor.FootprintUvs;
			}

			public void SetElevation(float metres, float scale)
			{
				_metres = metres;
				HasElevation = true;
				Apply(scale);
			}

			/// <summary>
			/// Writes the Y for the current map scale.
			///
			/// Divided by Scale alone, with no latitude compensation, because this has to match the
			/// surface the building stands on rather than be true to life: the terrain mesh places
			/// its surface at metres/Scale (elevation * UnityMapTile.TileScale, times the tile's
			/// uniform localScale). The landmark's own geometry uses metres/(Scale * cos(latitude)),
			/// so a landmark ends up taller per metre than the relief it sits on - the same split
			/// vector buildings already have between HeightModifier and SnapTerrainModifier. Using
			/// the cos-corrected factor here instead would sink or float the base by that factor,
			/// which is the more visible error by far.
			///
			/// No tile-size division either, unlike the vector modifiers: the landmark tile root is
			/// scaled (tileSize, 1, tileSize), so the Y axis is already in Unity units.
			/// </summary>
			public void Apply(float scale)
			{
				if (!HasElevation || _transform == null || scale <= 0f) return;

				var local = _transform.localPosition;
				_transform.localPosition = new Vector3(local.x, _metres / scale, local.z);
			}
		}
	}
}
