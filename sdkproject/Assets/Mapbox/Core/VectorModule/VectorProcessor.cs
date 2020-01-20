using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.VectorTile.Geometry;
using UnityEngine;

namespace Mapbox.Core.VectorModule
{
	public class VectorProcessor
	{
		public Action<CanonicalTileId, List<MeshData>> MeshOutput = (tileId, meshList) => { };
		private VectorProcessorDataFetcher _dataFetcher;

		private string _tilesetId = "mapbox.mapbox-streets-v8";
		private float _tileSize = 100;
		private float _tileScale = 1;

		public VectorModuleMergedModifierStack ModifierStack;

		//for vector calls dependant on elevation data
		private Dictionary<CanonicalTileId, UnityTile> _waitingTiles;

		public VectorProcessor()
		{
			_dataFetcher = new VectorProcessorDataFetcher();
			_dataFetcher.DataRecieved += ProcessData;
			_waitingTiles = new Dictionary<CanonicalTileId, UnityTile>();
		}

		//used for loading before tile finished
		public void CreateVectorVisuals(List<UnwrappedTileId> unwrappedTileIds)
		{
			if (_dataFetcher == null)
			{
				_dataFetcher = new VectorProcessorDataFetcher();
				_dataFetcher.DataRecieved += ProcessData;
				_waitingTiles = new Dictionary<CanonicalTileId, UnityTile>();
			}

			foreach (var tileId in unwrappedTileIds)
			{
				_dataFetcher.FetchData(false, null, tileId.Canonical, _tilesetId);
			}
		}

		//used for loading after tile finished
		public void CreateVectorVisuals(UnityTile tile)
		{
			if (!_waitingTiles.ContainsKey(tile.CanonicalTileId))
			{
				_waitingTiles.Add(tile.CanonicalTileId, tile);
				_dataFetcher.FetchData(false, null, tile.CanonicalTileId, _tilesetId);
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

		private void ProcessData(CanonicalTileId tileId, Map.VectorTile vectorTile)
		{
			if (_waitingTiles.ContainsKey(tileId))
			{
				//post processing uses terrain elevation
				ProcessVectorData(tileId, vectorTile, (v) => SnapTerrain(v, _waitingTiles[tileId]));
			}
			else
			{
				//pre processing doesn't use elevation yet
				ProcessVectorData(tileId, vectorTile);
			}
		}

		private void ProcessVectorData(CanonicalTileId tileId, Map.VectorTile vectorTile, Func<Vector3, Vector3> snapTerrainFunc = null)
		{
			var worker = new Worker();
			worker.MeshWorkComplete += (sender, results) => { MeshOutput(results.TileId, results.MeshDataList); };
			worker.Start(new ThreadParamaters()
			{
				ModifierStack = ModifierStack,
				TileId = tileId,
				VectorTile = vectorTile,
				SnapTerrainFunction = snapTerrainFunc
			});
		}
	}

	public class Worker
	{
		public event EventHandler<ThreadResults> MeshWorkComplete = delegate { };

		public void Start(object parameters)
		{
			new Thread(CreateMeshes).Start(parameters);
		}

		private void CreateMeshes(object parameters)
		{
			var threadParameters = parameters as ThreadParamaters;
			var meshDataList = new List<MeshData>();
			var _tileSize = 100;
			var _tileScale = 1;

			var layer = threadParameters.VectorTile.Data.GetLayer("building");
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
						if (threadParameters.SnapTerrainFunction != null)
						{
							newPoint = threadParameters.SnapTerrainFunction(newPoint);
						}

						newPoints.Add(newPoint);
					}

					vfu.Points.Add(newPoints);
				}

				threadParameters.ModifierStack.Execute(threadParameters.TileId, vfu, new MeshData());
			}

			meshDataList.Add(threadParameters.ModifierStack.End(threadParameters.TileId));

			MeshWorkComplete(this, new ThreadResults()
			{
				TileId = threadParameters.TileId,
				MeshDataList = meshDataList
			});
		}
	}

	public class ThreadResults : EventArgs
	{
		public CanonicalTileId TileId;
		public List<MeshData> MeshDataList;
	}

	public class ThreadParamaters
	{
		public VectorModuleMergedModifierStack ModifierStack;
		public CanonicalTileId TileId;
		public Map.VectorTile VectorTile;
		public Func<Vector3, Vector3> SnapTerrainFunction;
	}
}
