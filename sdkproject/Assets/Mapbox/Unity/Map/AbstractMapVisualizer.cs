using Mapbox.Unity.Map.Interfaces;

namespace Mapbox.Unity.Map
{
	using System.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Platform;
	using UnityEngine.Serialization;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Interfaces;

	/// <summary>
	/// Map Visualizer
	/// Represents a map.Doesn’t contain much logic and at the moment, it creates requested tiles and relays them to the factories
	/// under itself.It has a caching mechanism to reuse tiles and does the tile positioning in unity world.
	/// Later we’ll most likely keep track of map features here as well to allow devs to query for features easier
	/// (i.e.query all buildings x meters around any restaurant etc).
	/// </summary>
	public abstract class AbstractMapVisualizer : ScriptableObject
	{
		[SerializeField]
		[NodeEditorElementAttribute("Factories")]
		[FormerlySerializedAs("_factories")]
		public List<AbstractTileFactory> Factories;

		protected IMapReadable _map;
		protected Dictionary<UnwrappedTileId, UnityTile> _activeTiles = new Dictionary<UnwrappedTileId, UnityTile>();
		protected Queue<UnityTile> _inactiveTiles = new Queue<UnityTile>();
		private int _counter;

		private ModuleState _state;
		public ModuleState State
		{
			get
			{
				return _state;
			}
			internal set
			{
				if (_state != value)
				{
					_state = value;
					OnMapVisualizerStateChanged(_state);
				}
			}
		}

		public IMapReadable Map { get { return _map; } }
		public Dictionary<UnwrappedTileId, UnityTile> ActiveTiles { get { return _activeTiles; } }
		public Dictionary<UnwrappedTileId, int> _tileProgress;

		public event Action<ModuleState> OnMapVisualizerStateChanged = delegate { };

		/// <summary>
		/// Gets the unity tile from unwrapped tile identifier.
		/// </summary>
		/// <returns>The unity tile from unwrapped tile identifier.</returns>
		/// <param name="tileId">Tile identifier.</param>
		public UnityTile GetUnityTileFromUnwrappedTileId(UnwrappedTileId tileId)
		{
			return _activeTiles[tileId];
		}

		/// <summary>
		/// Initializes the factories by passing the file source down, which is necessary for data (web/file) calls
		/// </summary>
		/// <param name="fileSource"></param>
		public virtual void Initialize(IMapReadable map, IFileSource fileSource)
		{
			_map = map;
			_tileProgress = new Dictionary<UnwrappedTileId, int>();

			// Allow for map re-use by recycling any active tiles.
			var activeTiles = _activeTiles.Keys.ToList();
			foreach (var tile in activeTiles)
			{
				DisposeTile(tile);
			}

			State = ModuleState.Initialized;

			foreach (var factory in Factories)
			{
				if (null == factory)
				{
					Debug.LogError("AbstractMapVisualizer: Factory is NULL");
				}
				else
				{
					factory.Initialize(fileSource);
					UnregisterEvents(factory);
					RegisterEvents(factory);
				}
			}
		}

		private void RegisterEvents(AbstractTileFactory factory)
		{
			//directly relaying to map visualizer event for now, nothing doing special
			factory.OnTileError += Factory_OnTileError;
		}

		private void UnregisterEvents(AbstractTileFactory factory)
		{
			factory.OnTileError -= Factory_OnTileError;
		}

		public virtual void Destroy()
		{
			if (Factories != null)
			{
				_counter = Factories.Count;
				for (int i = 0; i < _counter; i++)
				{
					if (Factories[i] != null)
					{
						UnregisterEvents(Factories[i]);
					}
				}
			}

			// Inform all downstream nodes that we no longer need to process these tiles.
			// This scriptable object may be re-used, but it's gameobjects are likely
			// to be destroyed by a scene change, for example.
			foreach (var tileId in _activeTiles.Keys.ToList())
			{
				DisposeTile(tileId);
			}

			_activeTiles.Clear();
			_inactiveTiles.Clear();
		}

		#region Factory event callbacks
		//factory event callback, not relaying this up for now

		public virtual void TileStateChanged(UnityTile tile)
		{
			bool rasterDone = (tile.RasterDataState == TilePropertyState.None ||
								tile.RasterDataState == TilePropertyState.Loaded ||
								tile.RasterDataState == TilePropertyState.Error ||
								tile.RasterDataState == TilePropertyState.Cancelled);

			bool terrainDone = (tile.HeightDataState == TilePropertyState.None ||
								tile.HeightDataState == TilePropertyState.Loaded ||
								 tile.HeightDataState == TilePropertyState.Error ||
								 tile.HeightDataState == TilePropertyState.Cancelled);
			bool vectorDone = (tile.VectorDataState == TilePropertyState.None ||
								tile.VectorDataState == TilePropertyState.Loaded ||
								tile.VectorDataState == TilePropertyState.Error ||
								tile.VectorDataState == TilePropertyState.Cancelled);

			if (rasterDone && terrainDone && vectorDone)
			{
				tile.TileState = MeshGeneration.Enums.TilePropertyState.Loaded;
				//tile.gameObject.SetActive(true);

				// Check if all tiles in extent are active tiles
				if (_map.CurrentExtent.Count == _activeTiles.Count)
				{
					bool allDone = true;
					// Check if all tiles are loaded. 
					foreach (var currentTile in _map.CurrentExtent)
					{
						allDone = allDone && (_activeTiles.ContainsKey(currentTile) && _activeTiles[currentTile].TileState == TilePropertyState.Loaded);
					}

					if (allDone)
					{
						State = ModuleState.Finished;
					}
					else
					{
						State = ModuleState.Working;
					}
				}
				else
				{
					State = ModuleState.Working;
				}
			}
		}
		#endregion

		/// <summary>
		/// Registers requested tiles to the factories
		/// </summary>
		/// <param name="tileId"></param>
		public virtual UnityTile LoadTile(UnwrappedTileId tileId)
		{
			UnityTile unityTile = null;

			if (_inactiveTiles.Count > 0)
			{
				unityTile = _inactiveTiles.Dequeue();
			}

			if (unityTile == null)
			{
				unityTile = new GameObject().AddComponent<UnityTile>();
				unityTile.MeshRenderer.material = _map.TileMaterial;
				unityTile.transform.SetParent(_map.Root, false);
			}

			unityTile.Initialize(_map, tileId, _map.WorldRelativeScale, _map.AbsoluteZoom, _map.LoadingTexture);
			PlaceTile(tileId, unityTile, _map);

			// Don't spend resources naming objects, as you shouldn't find objects by name anyway!
#if UNITY_EDITOR
			unityTile.gameObject.name = unityTile.CanonicalTileId.ToString();
#endif
			unityTile.OnHeightDataChanged += TileStateChanged;
			unityTile.OnRasterDataChanged += TileStateChanged;
			unityTile.OnVectorDataChanged += TileStateChanged;

			unityTile.TileState = MeshGeneration.Enums.TilePropertyState.Loading;
			ActiveTiles.Add(tileId, unityTile);

			foreach (var factory in Factories)
			{
				factory.Register(unityTile);
			}

			return unityTile;
		}

		public virtual void DisposeTile(UnwrappedTileId tileId)
		{
			var unityTile = ActiveTiles[tileId];

			foreach (var factory in Factories)
			{
				factory.Unregister(unityTile);
			}

			unityTile.Recycle();
			ActiveTiles.Remove(tileId);
			_inactiveTiles.Enqueue(unityTile);
		}

		/// <summary>
		/// Repositions active tiles instead of recreating them. Useful for panning the map
		/// </summary>
		/// <param name="tileId"></param>
		public virtual void RepositionTile(UnwrappedTileId tileId)
		{
			UnityTile currentTile;
			if (ActiveTiles.TryGetValue(tileId, out currentTile))
			{
				PlaceTile(tileId, currentTile, _map);
			}
		}

		protected abstract void PlaceTile(UnwrappedTileId tileId, UnityTile tile, IMapReadable map);

		#region Events
		/// <summary>
		/// The  <c>OnTileError</c> event triggers when there's a <c>Tile</c> error.
		/// Returns a <see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance as a parameter, for the tile on which error occurred.
		/// </summary>
		public event EventHandler<TileErrorEventArgs> OnTileError;
		private void Factory_OnTileError(object sender, TileErrorEventArgs e)
		{
			EventHandler<TileErrorEventArgs> handler = OnTileError;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		public void ReregisterAllTiles()
		{
			foreach (var activeTile in _activeTiles)
			{
				foreach (var abstractTileFactory in Factories)
				{
					abstractTileFactory.Register(activeTile.Value);
				}
			}
		}

		public void UnregisterAllTiles()
		{
			foreach (var activeTile in _activeTiles)
			{
				foreach (var abstractTileFactory in Factories)
				{
					abstractTileFactory.Unregister(activeTile.Value);
				}
			}
		}

		public void UnregisterTilesFrom(AbstractTileFactory factory)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.Unregister(tileBundle.Value);
			}
		}

		public void UnregisterAndRedrawTilesFromLayer(VectorTileFactory factory, LayerVisualizerBase layerVisualizer)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.UnregisterLayer(tileBundle.Value, layerVisualizer);
			}
			layerVisualizer.UnbindSubLayerEvents();
			layerVisualizer.SetProperties(layerVisualizer.SubLayerProperties);
			layerVisualizer.InitializeStack();
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.RedrawSubLayer(tileBundle.Value, layerVisualizer);
			}
		}

		public void RemoveTilesFromLayer(VectorTileFactory factory, LayerVisualizerBase layerVisualizer)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.UnregisterLayer(tileBundle.Value, layerVisualizer);
			}
			factory.RemoveVectorLayerVisualizer(layerVisualizer);
		}

		public void ReregisterTilesTo(VectorTileFactory factory)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.Register(tileBundle.Value);
			}
		}

		public void UpdateTileForProperty(AbstractTileFactory factory, LayerUpdateArgs updateArgs)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.UpdateTileProperty(tileBundle.Value, updateArgs);
			}
		}

		public void ClearCaches()
		{
			foreach (var abstractTileFactory in Factories)
			{
				abstractTileFactory.Reset();
			}
		}
		#endregion
	}
}
