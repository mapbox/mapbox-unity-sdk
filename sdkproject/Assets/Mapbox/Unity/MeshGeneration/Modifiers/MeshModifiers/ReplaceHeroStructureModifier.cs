namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.VectorTile.Geometry;

	public class ReplaceHeroStructureModifier : GameObjectModifier, IReplacementCriteria
	{
		public List<HeroStructureData> heroStructures = new List<HeroStructureData>();



		private Dictionary<string, List<HeroStructureData>> _heroStructureTileIdDictionary;


		private List<ReplaceFeatureModifier> _replaceFeatureModifiers = new List<ReplaceFeatureModifier>();

		private int _numHeroStructures;
		private int _numSpawned;

		private List<List<Point2d<float>>> _geom;

		public override void Initialize()
		{
			base.Initialize();

			AbstractMap map = FindObjectOfType<AbstractMap>();
			int zoom = map.AbsoluteZoom;
			_heroStructureTileIdDictionary = new Dictionary<string, List<HeroStructureData>>();

			for (int i = 0; i < heroStructures.Count; i++)
			{
				if(!heroStructures[i].active)
				{
					continue;
				}

				HeroStructureData heroStructureData = new HeroStructureData();

				heroStructureData.prefab = heroStructures[i].prefab;
				heroStructureData.latLon = heroStructures[i].latLon;

				heroStructureData.SetLatLonVector2d();
				heroStructureData.SetRadius();

				Vector2d heroLatLon = heroStructureData.LatLonVector2d;

				string tileId = Conversions.LatitudeLongitudeToTileId(heroLatLon.x, heroLatLon.y, zoom).ToString();

				if (!_heroStructureTileIdDictionary.ContainsKey(tileId))
				{
					_heroStructureTileIdDictionary.Add(tileId, new List<HeroStructureData>());
				}

				_heroStructureTileIdDictionary[tileId].Add(heroStructureData);
				_numHeroStructures++;
			}
		}

		public bool ShouldReplaceFeature(VectorFeatureUnity feature)
		{
			return CheckHeroStructures(feature, ReplaceCheck) != null;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			if (_numSpawned == _numHeroStructures || ve.Feature == null)
			{
				return;
			}
			HeroStructureData heroStructureData = CheckHeroStructures(ve.Feature, SpawnCheck);
			if (heroStructureData != null)
			{
				SpawnHeroStructure(ve, heroStructureData);
			}
		}

		private Vector3 GetCentroidVector(VectorFeatureUnity feature)
		{
			Vector3 centroidVector = new Vector3();
			for (int i = 0; i < feature.Points[0].Count; i++)
			{
				centroidVector += feature.Points[0][i];
			}
			return centroidVector / feature.Points[0].Count;
		}

		private void SpawnHeroStructure(VectorEntity ve, HeroStructureData heroStructureData)
		{
			GameObject prefab = heroStructureData.prefab;
			GameObject go = Instantiate(prefab) as GameObject;

			Debug.Log("Spawning " + prefab.name);

			go.name = ve.Feature.Data.Id.ToString();

			go.transform.SetParent(ve.GameObject.transform, false);

			Vector3 centroidVector = GetCentroidVector(ve.Feature);

			go.transform.localPosition = centroidVector;

			heroStructureData.HasSpawned = true;
			_numSpawned++;
		}

		public HeroStructureData CheckHeroStructures(VectorFeatureUnity feature, Func<VectorFeatureUnity, HeroStructureData, bool> func)
		{
			string tileId = feature.Tile.UnwrappedTileId.ToString();
			List<HeroStructureData> heroStructureDataList;
			if (_heroStructureTileIdDictionary.TryGetValue(tileId, out heroStructureDataList))
			{
				for (int i = 0; i < heroStructureDataList.Count; i++)
				{
					HeroStructureData heroStructureData = heroStructureDataList[i];
					if (func(feature, heroStructureData))
					{
						return heroStructureData;
					}
				}
			}
			return null;
		}

		private bool SpawnCheck(VectorFeatureUnity feature, HeroStructureData heroStructureData)
		{
			if (!heroStructureData.HasSpawned)
			{
				if (feature.ContainsLatLon(heroStructureData.LatLonVector2d))
				{
					return true;
				}
			}
			return false;
		}

		private bool ReplaceCheck(VectorFeatureUnity feature, HeroStructureData heroStructureData)
		{
			var from = Conversions.LatitudeLongitudeToUnityTilePosition(heroStructureData.LatLonVector2d, feature.Tile);
			//var to = GetCentroidVector(feature);//).Points[0][0];
			var to = feature.Points[0][0];

			//_geom = feature.Data.Geometry<float>();

			//for (int i = 0; i < feature.Points[0].Count; i++)
			//{

				//centroidVector += feature.Points[0][i];
			//}

			//bool isInPoly = PolygonUtils.PointInPolygon(new Point2d<float>(from.x, from.y), feature.Data.Geometry<float>());

			//return isInPoly;
			float sqrMag = Vector2.SqrMagnitude(new Vector2(from.x, from.y) - new Vector2(to.x, to.z));

			return (sqrMag < heroStructureData.Radius);
		}
	}
}
