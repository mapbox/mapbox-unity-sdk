namespace Mapbox.Unity.MeshGeneration.Factories
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Map;
	using Mapbox.Unity.Utilities;
	using System;

	/// <summary>
	/// Uses vector tile api to visualize vector data.
	/// Fetches the vector data for given tile and passes layer data to layer visualizers.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Factories/Vector Tile Factory - Style Optimized")]
	public class StyleOptimizedVectorTileFactory : AbstractTileFactory
	{
		[SerializeField]
		private string _mapId = "";

		[SerializeField]
		[StyleSearch]
		Style _optimizedStyle;

		[NodeEditorElementAttribute("Layer Visalizers")]
		public List<LayerVisualizerBase> Visualizers;

		private Dictionary<string, List<LayerVisualizerBase>> _layerBuilder;
		private Dictionary<UnityTile, StyleOptimizedVectorTile> _cachedData = new Dictionary<UnityTile, StyleOptimizedVectorTile>();

		public string MapId
		{
			get
			{
				return _mapId;
			}

			set
			{
				_mapId = value;
			}
		}

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
			var vectorTile = new StyleOptimizedVectorTile(_optimizedStyle.Id, _optimizedStyle.Modified);
			tile.AddTile(vectorTile);

			Progress++;
			vectorTile.Initialize(_fileSource, tile.CanonicalTileId, _mapId, () =>
			{
				if (tile == null)
				{
					return;
				}

				if (vectorTile.HasError)
				{
					OnErrorOccurred(new TileErrorEventArgs(tile.CanonicalTileId, vectorTile.GetType(), tile, vectorTile.Exceptions));
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

		/// <summary>
		/// Method to be called when a tile error has occurred.
		/// </summary>
		/// <param name="e"><see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance/</param>
		protected override void OnErrorOccurred(TileErrorEventArgs e)
		{
			base.OnErrorOccurred(e);
		}

		internal override void OnUnregistered(UnityTile tile)
		{
			// We are no longer interested in this tile's notifications.
			tile.OnHeightDataChanged -= DataChangedHandler;
			tile.OnRasterDataChanged -= DataChangedHandler;

			// clean up any pending request for this tile
			if (_cachedData.ContainsKey(tile))
			{
				Progress--;
				_cachedData.Remove(tile);
			}

			foreach (var vis in Visualizers)
			{
				vis.UnregisterTile(tile);
			}
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
							builder.Create(_cachedData[tile].Data.GetLayer(layerName), tile, DecreaseProgressCounter);
						}
					}
				}
			}

			tile.VectorDataState = TilePropertyState.Loaded;
			Progress--;

			_cachedData.Remove(tile);
		}

		private void DecreaseProgressCounter()
		{
			Progress--;
		}
	}

	[Serializable]
	public class Style
	{
		public string Name;
		public string Id;
		public string Modified;
		public string UserName;
	}
}
