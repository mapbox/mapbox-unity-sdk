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

	/// <summary>
	/// ReplaceBuildingFeatureModifier takes in POIs and checks if the feature layer has those points and deletes them
	/// </summary>
	//[CreateAssetMenu(menuName = "Mapbox/Modifiers/Replace Feature Modifier")]

	public class HeroStructureDataBundleCollection
	{
		private const int MAX_BUNDLE_SIZE = 100;

		public HeroStructureDataBundle[] heroStructureDataBundleArray = new HeroStructureDataBundle[MAX_BUNDLE_SIZE];
		public int count;

		public void Add(HeroStructureDataBundle heroStructureDataBundle)
		{
			if(count >= MAX_BUNDLE_SIZE)
			{
				Debug.Log("Max bundle size reached!");
				return;
			}
			heroStructureDataBundleArray[count] = heroStructureDataBundle;
			count++;
		}
	}

	public class ReplaceHeroStructureModifier : GameObjectModifier, IReplacementCriteria
	{
		public HeroStructureCollection heroStructureCollection;

		private List<string> _latLonToSpawn;

		private Dictionary<ulong, GameObject> _objects;
		private GameObject _poolGameObject;
		[SerializeField]
		private SpawnPrefabOptions _options;
		private List<GameObject> _prefabList = new List<GameObject>();

		[SerializeField]
		[Geocode]
		private List<string> _prefabLocations;

		[SerializeField]
		private List<string> _explicitlyBlockedFeatureIds;
		//maximum distance to trigger feature replacement ( in tile space )
		private const float _maxDistanceToBlockFeature_tilespace = 1000f;

		/// <summary>
		/// List of featureIds to test against. 
		/// We need a list of featureIds per location. 
		/// A list is required since buildings on tile boundary will have multiple id's for the same feature.
		/// </summary>
		private List<List<string>> _featureId;
		private string _tempFeatureId;

		private List<HeroStructureDataBundle> _heroStructuresToSpawn;


		private List<HeroStructureDataBundle> _heroStructureBuffer;


		private Dictionary<string, HeroStructureDataBundleCollection> _heroStructureTileIdDictionary;

		private string _lastTileId;

		public override void Initialize()
		{
			base.Initialize();

			AbstractMap abstractMap = FindObjectOfType<AbstractMap>();

			CreateTileIdHeroBundleDictionary(abstractMap.AbsoluteZoom);

			//duplicate the list of lat/lons to track which coordinates have already been spawned
			_latLonToSpawn = new List<string>(_prefabLocations);
			_featureId = new List<List<string>>();
			for (int i = 0; i < _prefabLocations.Count; i++)
			{
				_featureId.Add(new List<string>());
			}
			if (_objects == null)
			{
				_objects = new Dictionary<ulong, GameObject>();
				_poolGameObject = new GameObject("_inactive_prefabs_pool");
			}
			_latLonToSpawn = new List<string>(_prefabLocations);
		}

		private void CreateTileIdHeroBundleDictionary(int zoom)
		{
			_heroStructureTileIdDictionary = new Dictionary<string, HeroStructureDataBundleCollection>();

			heroStructureCollection = Resources.Load("GlobalHeroStructureCollection") as HeroStructureCollection;
			for (int i = 0; i < heroStructureCollection.heroStructures.Count; i++)
			{
				HeroStructureDataBundle heroStructureDataBundle = heroStructureCollection.heroStructures[i];
				Vector2d heroStructureLatLon = heroStructureDataBundle.latLon_vector2d;

				heroStructureDataBundle.Spawned = false;

				string tileId = Conversions.LatitudeLongitudeToTileId(heroStructureLatLon.x, heroStructureLatLon.y, zoom).ToString();
				if (!_heroStructureTileIdDictionary.ContainsKey(tileId))
				{
					_heroStructureTileIdDictionary.Add(tileId, new HeroStructureDataBundleCollection());
				}
				_heroStructureTileIdDictionary[tileId].Add(heroStructureDataBundle);
			}
		}


		public override void SetProperties(ModifierProperties properties)
		{
			_options = (SpawnPrefabOptions)properties;
		}

		public void SetLatLon(string latLon)
		{
			_prefabLocations = new List<string>();
			_prefabLocations.Add(latLon);
		}

		//heroStructures

		/// <summary>
		/// Check the feature against the list of lat/lons in the modifier
		/// </summary>
		/// <returns><c>true</c>, if the feature overlaps with a lat/lon in the modifier <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		/// 
		/// 

		private HeroStructureDataBundleCollection GetHeroStructureDataBundle(VectorFeatureUnity feature)//, Fuct function)
		{
			string tileId = feature.Tile.UnwrappedTileId.ToString();
			HeroStructureDataBundleCollection heroStructureDataBundleCollection;
			if (_heroStructureTileIdDictionary.TryGetValue(tileId, out heroStructureDataBundleCollection))
			{
				//put 


				return heroStructureDataBundleCollection;
			}
			return null;
		}

		public bool ShouldReplaceFeature(VectorFeatureUnity feature)
		{
			HeroStructureDataBundleCollection heroStructureDataBundleCollection = GetHeroStructureDataBundle(feature);
			if (heroStructureDataBundleCollection != null)
			{
				HeroStructureDataBundle[] heroStructureDataBundleArray = heroStructureDataBundleCollection.heroStructureDataBundleArray;
				int count = heroStructureDataBundleCollection.count;
				for (int i = 0; i < count; i++)
				{
					HeroStructureDataBundle heroStructureDataBundle = heroStructureDataBundleArray[i];

					var from = Conversions.LatitudeLongitudeToUnityTilePosition(heroStructureDataBundle.latLon_vector2d, feature.Tile);

					var to = feature.Points[0][0];

					float sqrMag = Vector2.SqrMagnitude(new Vector2(from.x, from.y) - new Vector2(to.x, to.z));

					if (sqrMag < heroStructureDataBundle.radius)
					{
						return true;
					}
				}
			}
			return false;
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

			HeroStructureDataBundleCollection heroStructureDataBundleCollection = GetHeroStructureDataBundle(ve.Feature);
			if (heroStructureDataBundleCollection == null)
			{
				return;
			}
			HeroStructureDataBundle[] heroStructureDataBundleArray = heroStructureDataBundleCollection.heroStructureDataBundleArray;
			int count = heroStructureDataBundleCollection.count;
			for (int i = 0; i < count; i++)
			{
				HeroStructureDataBundle heroStructureDataBundle = heroStructureDataBundleArray[i];
				if (heroStructureDataBundle.Spawned)
				{
					continue;
				}
				if (ve.Feature.ContainsLatLon(heroStructureDataBundle.latLon_vector2d))
				{
					SpawnHeroStructure(ve, tile, heroStructureDataBundle);
				}
			}
		}

		public T GenericMethod<T>(T param)
		{
			return param;
		}


		//private bool SpawnCheck

		private void SpawnHeroStructure(VectorEntity ve, UnityTile tile, HeroStructureDataBundle heroStructureDataBundle)
		{
			
			GameObject goPrefab = heroStructureDataBundle.prefab;
			GameObject go = Instantiate(goPrefab) as GameObject;
			go.name = goPrefab.name;
			go.transform.SetParent(ve.GameObject.transform, false);
			PositionScaleRectTransform(ve, tile, go);

			heroStructureDataBundle.Spawned = true;

			//if (_options.AllPrefabsInstatiated != null)
			//{
			//	_options.AllPrefabsInstatiated(_prefabList);
			//}
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


		public override void OnPoolItem(VectorEntity vectorEntity)
		{
			base.OnPoolItem(vectorEntity);
			var featureId = vectorEntity.Feature.Data.Id;

			if (!_objects.ContainsKey(featureId))
			{
				return;
			}

			var go = _objects[featureId];
			if (go == null || _poolGameObject == null)
			{
				return;
			}

			go.SetActive(false);
			go.transform.SetParent(_poolGameObject.transform, false);
		}
	}
}
