namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	//using Mapbox.VectorTile.Geometry;
	//using Mapbox.Unity.MeshGeneration.Interfaces;

	public class ReplaceHeroStructureModifier : GameObjectModifier, IReplacementCriteria
	{

		private class HeroStructureDataCollection
		{
			private const int _MAX_BUNDLE_SIZE = 100;

			public HeroStructureData[] heroStructureData = new HeroStructureData[_MAX_BUNDLE_SIZE];
			public int count;

			public void Add(HeroStructureData data)
			{
				if (count == _MAX_BUNDLE_SIZE)
				{
					Debug.LogError("Max bundle size reached!");
					return;
				}
				heroStructureData[count] = data;
				count++;
			}
		}

		public List<HeroStructureData> heroStructures = new List<HeroStructureData>();

		private Dictionary<string, HeroStructureDataCollection> _heroStructureTileIdDictionary;

		private int _numHeroStructures;
		private int _numSpawned;

		public override void Initialize()
		{
			base.Initialize();

			AbstractMap map = FindObjectOfType<AbstractMap>();

			int zoom = map.AbsoluteZoom;

			_heroStructureTileIdDictionary = new Dictionary<string, HeroStructureDataCollection>();

			for (int i = 0; i < heroStructures.Count; i++)
			{
				HeroStructureData heroStructureData = heroStructures[i];
				Vector2d heroStructureLatLon = heroStructureData.LatLonVector2d;

				heroStructureData.Spawned = false;

				string tileId = Conversions.LatitudeLongitudeToTileId(heroStructureLatLon.x, heroStructureLatLon.y, zoom).ToString();
				if (!_heroStructureTileIdDictionary.ContainsKey(tileId))
				{
					_heroStructureTileIdDictionary.Add(tileId, new HeroStructureDataCollection());
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

		private void SpawnHeroStructure(VectorEntity ve, HeroStructureData heroStructureData)
		{
			GameObject prefab = heroStructureData.prefab;
			GameObject go = Instantiate(prefab) as GameObject;

			go.name = ve.Feature.Data.Id.ToString();

			go.transform.SetParent(ve.GameObject.transform, false);

			var centroidVector = new Vector3();
			foreach (var point in ve.Feature.Points[0])
			{
				centroidVector += point;
			}
			centroidVector = centroidVector / ve.Feature.Points[0].Count;

			go.transform.localPosition = centroidVector;

			heroStructureData.Spawned = true;
			_numSpawned++;
		}

		public HeroStructureData CheckHeroStructures(VectorFeatureUnity feature, Func<VectorFeatureUnity, HeroStructureData, bool> func )
		{
			string tileId = feature.Tile.UnwrappedTileId.ToString();
			HeroStructureDataCollection heroStructureDataCollection;
			if (_heroStructureTileIdDictionary.TryGetValue(tileId, out heroStructureDataCollection))
			{
				int count = heroStructureDataCollection.count;
				for (int i = 0; i < count; i++)
				{
					HeroStructureData heroStructureData = heroStructureDataCollection.heroStructureData[i];
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
