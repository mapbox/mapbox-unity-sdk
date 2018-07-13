namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Map;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;
	using Mapbox.VectorTile.Geometry;
	using Mapbox.Unity.MeshGeneration.Interfaces;

	public class ReplaceHeroStructureModifier : GameObjectModifier, IReplacementCriteria
	{

		private class HeroStructureDataCollection
		{
			private const int MAX_BUNDLE_SIZE = 100;

			private HeroStructureData[] heroStructureDataArray = new HeroStructureData[MAX_BUNDLE_SIZE];
			private int _count;

			public HeroStructureData[] HeroStructureDataArray 
			{
				get
				{
					return heroStructureDataArray;
				}
			}

			public int Count
			{
				get
				{
					return _count;
				}
			}

			public void Add(HeroStructureData heroStructureData)
			{
				if (_count >= MAX_BUNDLE_SIZE)
				{
					Debug.Log("Max bundle size reached!");
					return;
				}
				heroStructureDataArray[_count] = heroStructureData;
				_count++;
			}
		}

		public List<HeroStructureData> heroStructures = new List<HeroStructureData>();

		//[SerializeField]
		//private SpawnPrefabOptions _options;

		private Dictionary<string, HeroStructureDataCollection> _heroStructureTileIdDictionary;

		public override void Initialize()
		{
			base.Initialize();

			AbstractMap map = FindObjectOfType<AbstractMap>();

			CreateTileIdHeroBundleDictionary(map.AbsoluteZoom);

		}

		private void CreateTileIdHeroBundleDictionary(int zoom)
		{
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
			}
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			ShouldSpawnFeature(ve, tile);
		}

		private void ShouldSpawnFeature(VectorEntity ve, UnityTile tile)
		{
			if (ve.Feature == null)
			{
				return;
			}

			HeroStructureDataCollection heroStructureDataBundleCollection = GetHeroStructureDataCollection(ve.Feature);
			if (heroStructureDataBundleCollection == null)
			{
				return;
			}
			HeroStructureData[] heroStructureDataBundleArray = heroStructureDataBundleCollection.HeroStructureDataArray;
			int count = heroStructureDataBundleCollection.Count;
			for (int i = 0; i < count; i++)
			{
				HeroStructureData heroStructureDataBundle = heroStructureDataBundleArray[i];
				if (heroStructureDataBundle.Spawned)
				{
					continue;
				}
				if (ve.Feature.ContainsLatLon(heroStructureDataBundle.LatLonVector2d))
				{
					SpawnHeroStructure(ve, tile, heroStructureDataBundle);
				}
			}
		}

		public bool ShouldReplaceFeature(VectorFeatureUnity feature)
		{
			HeroStructureDataCollection heroStructureDataBundleCollection = GetHeroStructureDataCollection(feature);
			if (heroStructureDataBundleCollection != null)
			{
				HeroStructureData[] heroStructureDataBundleArray = heroStructureDataBundleCollection.HeroStructureDataArray;
				int count = heroStructureDataBundleCollection.Count;
				for (int i = 0; i < count; i++)
				{
					HeroStructureData heroStructureData = heroStructureDataBundleArray[i];

					var from = Conversions.LatitudeLongitudeToUnityTilePosition(heroStructureData.LatLonVector2d, feature.Tile);

					var to = feature.Points[0][0];

					float sqrMag = Vector2.SqrMagnitude(new Vector2(from.x, from.y) - new Vector2(to.x, to.z));

					if (sqrMag < heroStructureData.Radius)
					{
						return true;
					}
				}
			}
			return false;
		}

		private HeroStructureDataCollection GetHeroStructureDataCollection(VectorFeatureUnity feature)//, Fuct function)
		{
			string tileId = feature.Tile.UnwrappedTileId.ToString();
			HeroStructureDataCollection heroStructureDataBundleCollection;
			if (_heroStructureTileIdDictionary.TryGetValue(tileId, out heroStructureDataBundleCollection))
			{
				return heroStructureDataBundleCollection;
			}
			return null;
		}

		public bool CheckHeroStructures( Func<HeroStructureData> func )
		{

			return false;
		}

		private void SpawnHero(HeroStructureData heroStructureDataBundle)
		{
			
		}

		private void CullMesh(HeroStructureData heroStructureDataBundle)
		{

		}

		private void SpawnHeroStructure(VectorEntity ve, UnityTile tile, HeroStructureData heroStructureDataBundle)
		{
			
			GameObject goPrefab = heroStructureDataBundle.prefab;
			GameObject go = Instantiate(goPrefab) as GameObject;
			go.name = goPrefab.name;
			go.transform.SetParent(ve.GameObject.transform, false);
			PositionScaleRectTransform(ve, tile, go);

			heroStructureDataBundle.Spawned = true;
		}

		public void PositionScaleRectTransform(VectorEntity ve, UnityTile tile, GameObject go)
		{
			float sv = 1.0f;

			go.transform.localScale = new Vector3(sv, sv, sv);//_options.prefab.transform.localScale;
			RectTransform goRectTransform;
			IFeaturePropertySettable settable = null;
			var centroidVector = new Vector3();
			foreach (var point in ve.Feature.Points[0])
			{
				centroidVector += point;
			}
			centroidVector = centroidVector / ve.Feature.Points[0].Count;

			go.name = ve.Feature.Data.Id.ToString();

			goRectTransform = go.GetComponent<RectTransform>();
			if (goRectTransform == null)
			{
				go.transform.localPosition = centroidVector;
			}
			else
			{
				goRectTransform.anchoredPosition3D = centroidVector;
			}
			//go.transform.localScale = Constants.Math.Vector3One;

			settable = go.GetComponent<IFeaturePropertySettable>();
			if (settable != null)
			{
				settable.Set(ve.Feature.Properties);
			}
			//if (_options.scaleDownWithWorld)
			//{
			//	go.transform.localScale = (go.transform.localScale * (tile.TileScale));
			//}
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
