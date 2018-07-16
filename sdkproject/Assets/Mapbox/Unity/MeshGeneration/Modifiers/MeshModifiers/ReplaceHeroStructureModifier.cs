namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	public class ReplaceHeroStructureModifier : GameObjectModifier, IReplacementCriteria
	{
		public List<HeroStructureData> heroStructures = new List<HeroStructureData>();

		private Dictionary<string, List<HeroStructureData>> _heroStructureTileIdDictionary;

		private int _numHeroStructures;
		private int _numSpawned;

		public override void Initialize()
		{
			base.Initialize();

			AbstractMap map = FindObjectOfType<AbstractMap>();

			_heroStructureTileIdDictionary = new Dictionary<string, List<HeroStructureData>>();

			for (int i = 0; i < heroStructures.Count; i++)
			{
				HeroStructureData heroStructureData = heroStructures[i];
				Vector2d heroStructureLatLon = heroStructureData.LatLonVector2d;

				heroStructureData.Spawned = false;

				string tileId = Conversions.LatitudeLongitudeToTileId(heroStructureLatLon.x, heroStructureLatLon.y, map.AbsoluteZoom).ToString();
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
			return CheckHeroStructures(feature, Replace) != null;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			if (_numSpawned == _numHeroStructures || ve.Feature == null)
			{
				return;
			}
			HeroStructureData heroStructureData = CheckHeroStructures(ve.Feature, Spawn);
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

			heroStructureData.Spawned = true;
			_numSpawned++;
		}

		public HeroStructureData CheckHeroStructures(VectorFeatureUnity feature, Func<VectorFeatureUnity, HeroStructureData, bool> func )
		{
			string tileId = feature.Tile.UnwrappedTileId.ToString();
			List<HeroStructureData> heroStructureDataList;
			if (_heroStructureTileIdDictionary.TryGetValue(tileId, out heroStructureDataList))
			{
				for (int i = 0; i < heroStructureDataList.Count; i++)
				{
					HeroStructureData heroStructureData = heroStructureDataList[i];
					if(func(feature, heroStructureData))
					{
						return heroStructureData;
					}
				}
			}
			return null;
		}

		private bool Spawn(VectorFeatureUnity feature, HeroStructureData heroStructureData)
		{
			if (!heroStructureData.Spawned)
			{
				if (feature.ContainsLatLon(heroStructureData.LatLonVector2d))
				{
					return true;
				}
			}
			return false;
		}

		private bool Replace(VectorFeatureUnity feature, HeroStructureData heroStructureData)
		{
			var from = Conversions.LatitudeLongitudeToUnityTilePosition(heroStructureData.LatLonVector2d, feature.Tile);
			//var to = GetCentroidVector(feature);//).Points[0][0];
			var to = feature.Points[0][0];
			float sqrMag = Vector2.SqrMagnitude(new Vector2(from.x, from.y) - new Vector2(to.x, to.z));

			return (sqrMag < heroStructureData.Radius);
		}

		private void OnValidate()
		{
			for (int i = 0; i < heroStructures.Count; i++)
			{
				heroStructures[i].SetLatLonVector2d();
				heroStructures[i].SetRadius();
			}
		}
	}
}
