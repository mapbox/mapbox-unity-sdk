namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Map;
	using Mapbox.Platform;

	/// <summary>
	/// Uses vector tile api to visualize vector data.
	/// Fetches the vector data for given tile and passes layer data to layer visualizers.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Vector Tile Factory")]
	public class VectorTileFactory : AbstractTileFactory
	{
		[SerializeField]
		private string _mapId = "";

		public List<LayerVisualizerBase> Visualizers;

		private Dictionary<string, List<LayerVisualizerBase>> _layerBuilder;
		private Dictionary<UnityTile, VectorTile> _cachedData = new Dictionary<UnityTile, VectorTile>();

		public void OnEnable()
		{
			if (Visualizers == null)
			{
				Visualizers = new List<LayerVisualizerBase>();
			}
		}

		internal override void PreInitialize(WorldProperties wp)
		{
			base.PreInitialize(wp);
			foreach (LayerVisualizerBase layerviz in Visualizers)
			{
				layerviz.PreInitialize(wp);
			}
		}


		internal override void Initialize(WorldProperties wp, IFileSource fileSource)
		{
			base.Initialize(wp, fileSource);

			_layerBuilder = new Dictionary<string, List<LayerVisualizerBase>>();
			foreach (LayerVisualizerBase layerviz in Visualizers)
			{
				layerviz.Initialize(wp);
				if (_layerBuilder.ContainsKey(layerviz.Key))
				{
					_layerBuilder[layerviz.Key].Add(layerviz);
				}
				else
				{
					_layerBuilder.Add(layerviz.Key, new List<LayerVisualizerBase>() { layerviz });
				}
			}
		}

		internal override void OnRegistered(UnityTile tile)
		{
			var vectorTile = new VectorTile();
			tile.AddTile(vectorTile);

			Progress++;
			vectorTile.Initialize(_fileSource, tile.CanonicalTileId, _mapId, () =>
			{
				if (vectorTile.HasError)
				{
					tile.VectorDataState = TilePropertyState.Error;
					Progress--;
					return;
				}

				_cachedData.Add(tile, vectorTile);

				// FIXME: we can make the request BEFORE getting a response from these!
				if (tile.HeightDataState == TilePropertyState.Loading ||
					tile.RasterDataState == TilePropertyState.Loading)
				{
					tile.OnHeightDataChanged += DataChangedHandler;
					tile.OnRasterDataChanged += DataChangedHandler;
				}
				else
				{
					CreateMeshes(tile);
				}
			});
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			// We are no longer interested in this tile's notifications.
			tile.OnHeightDataChanged -= DataChangedHandler;
			tile.OnRasterDataChanged -= DataChangedHandler;
		}

		private void DataChangedHandler(UnityTile t)
		{
			if (t.RasterDataState != TilePropertyState.Loading &&
				t.HeightDataState != TilePropertyState.Loading)
			{
				CreateMeshes(t);
			}
		}

		/// <summary>
		/// Fetches the vector data and passes each layer to relevant layer visualizers
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="e"></param>
		private void CreateMeshes(UnityTile tile)
		{
			tile.OnHeightDataChanged -= DataChangedHandler;
			tile.OnRasterDataChanged -= DataChangedHandler;

			tile.VectorDataState = TilePropertyState.Loading;

			// TODO: move unitytile state registrations to layer visualizers. Not everyone is interested in this data
			// and we should not wait for it here!
			foreach (var layerName in _cachedData[tile].Data.LayerNames())
			{
				if (_layerBuilder.ContainsKey(layerName))
				{
					foreach (var builder in _layerBuilder[layerName])
					{
						if (builder.Active)
						{
							builder.Create(_cachedData[tile].Data.GetLayer(layerName), tile);
						}
					}
				}
			}

			tile.VectorDataState = TilePropertyState.Loaded;
			Progress--;

			_cachedData.Remove(tile);
		}
	}
}
