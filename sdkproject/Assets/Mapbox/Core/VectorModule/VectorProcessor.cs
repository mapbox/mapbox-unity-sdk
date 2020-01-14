using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.VectorTile.Geometry;
using UnityEngine;

namespace Mapbox.Core.VectorModule
{
	[Serializable]
	public class VectorProcessor
	{
		public Action<CanonicalTileId, List<MeshData>> MeshOutput = (tileId, meshList) => { };
		private VectorProcessorDataFetcher _dataFetcher;

		private string _tilesetId = "mapbox.mapbox-streets-v8";
		//private Dictionary<string, LayerVisualizerBase> _layerBuilder;

		private float _tileSize = 100;
		private float _tileScale = 1;

		[SerializeField] private VectorProcessorModifierStack _modifierStack;

		//for vector calls dependant on elevation data
		private Dictionary<CanonicalTileId, UnityTile> _waitingTiles = new Dictionary<CanonicalTileId, UnityTile>();

		public VectorProcessor()
		{
			_dataFetcher = new VectorProcessorDataFetcher();
			_dataFetcher.DataRecieved += ProcessData;
		}

		public void CreateVectorVisuals(List<UnwrappedTileId> unwrappedTileIds)
		{
			foreach (var tileId in unwrappedTileIds)
			{
				_dataFetcher.FetchData(false, null, tileId.Canonical, _tilesetId);
			}
		}

		public void CreateVectorVisuals(UnityTile tile)
		{
			if (!_waitingTiles.ContainsKey(tile.CanonicalTileId))
			{
				_waitingTiles.Add(tile.CanonicalTileId, tile);
				_dataFetcher.FetchData(false, null, tile.CanonicalTileId, _tilesetId);
			}
		}

		private void ProcessData(CanonicalTileId tileId, Map.VectorTile vectorTile)
		{
			if (_waitingTiles.ContainsKey(tileId))
			{
				FlatVectorProcessing(tileId, vectorTile, (v) => SnapTerrain(v, _waitingTiles[tileId]));
			}
			else
			{
				FlatVectorProcessing(tileId, vectorTile);
			}
		}

		private Vector3 SnapTerrain(Vector3 original, UnityTile tile)
		{
			var scaledX = tile.Rect.Size.x * tile.TileScale;
			var scaledY = tile.Rect.Size.y * tile.TileScale;

			var h = tile.QueryHeightData(
				(float) ((original.x + scaledX / 2) / scaledX),
				(float) ((original.z + scaledY / 2) / scaledY));
			return original + new Vector3(0, h, 0);
		}

		private void FlatVectorProcessing(CanonicalTileId tileId, Map.VectorTile vectorTile, Func<Vector3, Vector3> snapTerrainFunc = null)
		{
			var meshDataList = new List<MeshData>();

			var layer = vectorTile.Data.GetLayer("building");
			if (layer == null)
				return;

			var featureCount = layer.FeatureCount();
			for (int i = 0; i < featureCount; i++)
			{
				var featureRaw = layer.GetFeature(i);

				List<List<Point2d<float>>> geom = featureRaw.Geometry<float>(); //and we're not clipping by passing no parameters

				if (geom[0][0].X < 0 || geom[0][0].X > layer.Extent || geom[0][0].Y < 0 || geom[0][0].Y > layer.Extent)
				{
					continue;
				}

				var vfu = new VectorFeatureUnity();
				vfu.Properties = featureRaw.GetProperties();
				for (int j = 0; j < geom.Count; j++)
				{
					var pointCount = geom[j].Count;
					var newPoints = new List<Vector3>(pointCount);
					for (int k = 0; k < pointCount; k++)
					{
						var point = geom[j][k];
						var newPoint = new Vector3((point.X / layer.Extent * _tileSize - (_tileSize / 2)) * _tileScale, 0, ((layer.Extent - point.Y) / layer.Extent * _tileSize - (_tileSize / 2)) * _tileScale);
						if (snapTerrainFunc != null)
						{
							newPoint = snapTerrainFunc(newPoint);
						}
						newPoints.Add(newPoint);
					}

					vfu.Points.Add(newPoints);
				}

				meshDataList.Add(_modifierStack.Execute(vfu, new MeshData()));
			}


			MeshOutput(tileId, meshDataList);
		}
	}
}
