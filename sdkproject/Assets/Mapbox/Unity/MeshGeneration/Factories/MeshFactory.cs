namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Map;

	/// <summary>
	/// Uses vector tile api to visualize vector data.
	/// Fetches the vector data for given tile and passes layer data to layer visualizers.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Mesh Factory")]
	public class MeshFactory : AbstractTileFactory
	{
		[SerializeField]
		private string _mapId = "";
		public List<LayerVisualizerBase> Visualizers;

		private Dictionary<string, List<LayerVisualizerBase>> _layerBuilder;
		Dictionary<UnityTile, Tile> _tiles;

		public void OnEnable()
		{
			if (Visualizers == null)
			{
				Visualizers = new List<LayerVisualizerBase>();
			}
		}

		/// <summary>
		/// Sets up the Mesh Factory
		/// </summary>
		/// <param name="fs"></param>
		internal override void OnInitialized()
		{
			_tiles = new Dictionary<UnityTile, Tile>();

			_layerBuilder = new Dictionary<string, List<LayerVisualizerBase>>();
			foreach (LayerVisualizerBase factory in Visualizers)
			{
				if (_layerBuilder.ContainsKey(factory.Key))
				{
					_layerBuilder[factory.Key].Add(factory);
				}
				else
				{
					_layerBuilder.Add(factory.Key, new List<LayerVisualizerBase>() { factory });
				}
			}
		}

		internal override void OnRegistered(UnityTile tile)
		{
			Run(tile);
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			// TODO: simplify this across tile factories? Cancel tile, but only if it needs to be cancelled.
			if (_tiles.ContainsKey(tile))
			{
				_tiles[tile].Cancel();
				_tiles.Remove(tile);

			}
			// We are no longer interested in this tile's notifications.
			tile.OnHeightDataChanged -= HeightDataChangedHandler;
			tile.OnRasterDataChanged -= ImageDataChangedHandler;
		}

		/// <summary>
		/// Mesh Factory waits for both Height and Image data to be processed if they are requested
		/// </summary>
		/// <param name="tile"></param>
		private void Run(UnityTile tile)
		{
			// FIXME: we can make the request BEFORE getting a response from these!
			if (tile.HeightDataState == TilePropertyState.Loading ||
				tile.RasterDataState == TilePropertyState.Loading)
			{
				tile.OnHeightDataChanged += HeightDataChangedHandler;
				tile.OnRasterDataChanged += ImageDataChangedHandler;
			}
			else
			{
				CreateMeshes(tile);
			}
		}

		private void HeightDataChangedHandler(UnityTile t)
		{
			// FIXME: Not all mesh factories care about these things. Why wait?
			if (t.RasterDataState != TilePropertyState.Loading)
			{
				CreateMeshes(t);
			}
		}

		private void ImageDataChangedHandler(UnityTile t)
		{
			if (t.HeightDataState != TilePropertyState.Loading)
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
			tile.OnHeightDataChanged -= HeightDataChangedHandler;
			tile.OnRasterDataChanged -= ImageDataChangedHandler;

			var parameters = new Tile.Parameters
			{
				Fs = this.FileSource,
				Id = new CanonicalTileId(tile.Zoom, (int)tile.TileCoordinate.x, (int)tile.TileCoordinate.y),
				MapId = _mapId
			};

			tile.VectorDataState = TilePropertyState.Loading;

			var vectorTile = new VectorTile();

			_tiles.Add(tile, vectorTile);
			vectorTile.Initialize(parameters, () =>
			{
				if (vectorTile.HasError || vectorTile.CurrentState == Tile.State.Canceled)
				{
					tile.VectorDataState = TilePropertyState.Error;
					return;
				}

				_tiles.Remove(tile);
				foreach (var layerName in vectorTile.Data.LayerNames())
				{
					if (_layerBuilder.ContainsKey(layerName))
					{
						foreach (var builder in _layerBuilder[layerName])
						{
							if (builder.Active)
							{
								builder.Create(vectorTile.Data.GetLayer(layerName), tile);
							}
						}
					}
				}

				tile.VectorDataState = TilePropertyState.Loaded;
			});
		}
	}
}
