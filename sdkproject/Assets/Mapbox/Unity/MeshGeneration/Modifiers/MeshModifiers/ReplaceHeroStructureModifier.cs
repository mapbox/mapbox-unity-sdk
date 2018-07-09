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
	using Mapbox.Unity.MeshGeneration.Interfaces;

	/// <summary>
	/// ReplaceBuildingFeatureModifier takes in POIs and checks if the feature layer has those points and deletes them
	/// </summary>
	//[CreateAssetMenu(menuName = "Mapbox/Modifiers/Replace Feature Modifier")]
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

		private List<HeroStructureDataBundle> heroStructuresInRange;

		public override void Initialize()
		{
			base.Initialize();

			AbstractMap abstractMap = FindObjectOfType<AbstractMap>();
			MapOptions mapOptions = abstractMap.Options;
			heroStructureCollection = Resources.Load("GlobalHeroStructureCollection") as HeroStructureCollection;
			heroStructuresInRange = heroStructureCollection.GetListOfHeroStructuresInRange(mapOptions);

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

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (SpawnPrefabOptions)properties;
		}

		public void SetLatLon(string latLon)
		{
			_prefabLocations = new List<string>();
			_prefabLocations.Add(latLon);
		}

		public override void FeaturePreProcess(VectorFeatureUnity feature)
		{
			int index = -1;

			foreach (var point in _prefabLocations)
			{
				try
				{
					index++;
					var coord = Conversions.StringToLatLon(point);
					if (feature.ContainsLatLon(coord) && (feature.Data.Id != 0))
					{
						_featureId[index] = (_featureId[index] == null) ? new List<string>() : _featureId[index];
						_tempFeatureId = feature.Data.Id.ToString();
						_featureId[index].Add(_tempFeatureId.Substring(0, _tempFeatureId.Length - 3));
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		//heroStructures

		/// <summary>
		/// Check the feature against the list of lat/lons in the modifier
		/// </summary>
		/// <returns><c>true</c>, if the feature overlaps with a lat/lon in the modifier <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		public bool ShouldReplaceFeature(VectorFeatureUnity feature)
		{

			for (int i = 0; i < heroStructuresInRange.Count; i++)
			{
				string point = heroStructuresInRange[i].latLon;

				Vector2d ll = Conversions.StringToLatLon(point);
				var tileId = Conversions.LatitudeLongitudeToTileId(ll.x, ll.y, feature.Tile.InitialZoom);

				if(!tileId.Canonical.Equals(feature.Tile.CanonicalTileId))
				{
					continue;
				}

				float rad = heroStructuresInRange[i].radius;

				var from = Conversions.LatitudeLongitudeToUnityTilePosition(Conversions.StringToLatLon(point), feature.Tile);

				//TODO - is this the best 3d point to query from? Any way to get the center?
				var to = feature.Points[0][0];
				//TODO - refactor this to use Vector2.SqrMag; it will be faster...
				//if (Vector2.SqrMagnitude(new Vector2(from.x, from.y) - new Vector2(to.X, to.Y)) < Math.Pow(_maxDistanceToBlockFeature_tilespace, 2f))
				//float dist = Vector2.SqrMagnitude(new Vector2(from.x, from.y) - new Vector2(to.x, to.z));
				float dist = Vector2.Distance(new Vector2((float)from.x, (float)from.y),new Vector2(to.x, to.z));
				//double compare = rad;//Math.Pow(rad, 2f);

				if (dist < (double)rad)
				{
					return true;
				}

			}
			return false;
		}


		public override void Run(VectorEntity ve, UnityTile tile)
		{
			//replace the feature only once per lat/lon
			int shouldSpawn = ShouldSpawnFeature(ve.Feature);
			if (shouldSpawn != -1)
			{
				GameObject gameObject = heroStructuresInRange[shouldSpawn].prefab;
				SpawnPrefab(ve, tile, gameObject);
			}
		}

		private void SpawnPrefab(VectorEntity ve, UnityTile tile, GameObject goPrefab)
		{
			//GameObject go;

			var featureId = ve.Feature.Data.Id;
			/*
			if (_objects.ContainsKey(featureId))
			{
				go = _objects[featureId];
				go.SetActive(true);
				go.transform.SetParent(ve.GameObject.transform, false);
			}
			else
			{
				go = Instantiate(_options.prefab);
				_prefabList.Add(go);
				_objects.Add(featureId, go);
				go.transform.SetParent(ve.GameObject.transform, false);
			}
			*/
			Debug.Log("SPAWNING!!! " +  goPrefab.name);
			GameObject go = Instantiate(goPrefab) as GameObject;
			go.name = goPrefab.name;
			go.transform.SetParent(ve.GameObject.transform, false);
			PositionScaleRectTransform(ve, tile, go);

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

		/// <summary>
		/// Checks if the feature should be used to spawn a prefab, once per lat/lon
		/// </summary>
		/// <returns><c>true</c>, if the feature should be spawned <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>

		/*
		private bool ShouldSpawnFeature(VectorFeatureUnity feature)
		{
			if (feature == null)
			{
				return false;
			}

			if (_objects.ContainsKey(feature.Data.Id))
			{
				return true;
			}

			foreach (var point in _latLonToSpawn)
			{
				var coord = Conversions.StringToLatLon(point);
				if (feature.ContainsLatLon(coord))
				{
					_latLonToSpawn.Remove(point);
					return true;
				}
			}

			return false;
		}
		*/
		private int ShouldSpawnFeature(VectorFeatureUnity feature)
		{
			if (feature == null)
			{
				return -1;
			}

			for (int i = 0; i < heroStructuresInRange.Count; i++)
			{
				HeroStructureDataBundle heroStructureDataBundle = heroStructuresInRange[i];
				var coord = Conversions.StringToLatLon(heroStructureDataBundle.latLon);
				if (feature.ContainsLatLon(coord))
				{
					//heroStructures.Remove(heroStructureDataBundle);
					//_latLonToSpawn.Remove(point);
					return i;
				}
			}

			return -1;
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
