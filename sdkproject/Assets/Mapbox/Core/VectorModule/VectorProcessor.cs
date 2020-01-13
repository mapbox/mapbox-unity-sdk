using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.VectorTile.Geometry;
using Unity.UNetWeaver;
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

		private void ProcessData(CanonicalTileId tileId, Map.VectorTile vectorTile)
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
						newPoints.Add(new Vector3((float) (point.X / layer.Extent * _tileSize - (_tileSize / 2)) * _tileScale, 0, (float) ((layer.Extent - point.Y) / layer.Extent * _tileSize - (_tileSize / 2)) * _tileScale));
					}

					vfu.Points.Add(newPoints);
				}

				meshDataList.Add(_modifierStack.Execute(vfu, new MeshData()));
			}


			MeshOutput(tileId, meshDataList);
		}
	}

	public class VectorProcessorDataFetcher
	{
		public Action<string> ErrorRecieved = (s) => { };
		public Action<CanonicalTileId, Map.VectorTile> DataRecieved = (tileId, vectorTile) => { };

		protected MapboxAccess _fileSource;

		public void FetchData(bool useOptimizedStyle, Style style, CanonicalTileId tileId, string tilesetId)
		{
			if (_fileSource == null)
				_fileSource = MapboxAccess.Instance;

			var vectorTile = (useOptimizedStyle) ? new Map.VectorTile(style.Id, style.Modified) : new Map.VectorTile();
			vectorTile.Initialize(_fileSource, tileId, tilesetId, () =>
			{
				if (vectorTile.HasError)
				{
					//FetchingError(vectorDaraParameters.tile, vectorTile, new TileErrorEventArgs(vectorDaraParameters.tile.CanonicalTileId, vectorTile.GetType(), vectorDaraParameters.tile, vectorTile.Exceptions));
					ErrorRecieved(vectorTile.ExceptionsAsString);
				}
				else
				{
					DataRecieved(tileId, vectorTile);
				}
			});
		}
	}
}
