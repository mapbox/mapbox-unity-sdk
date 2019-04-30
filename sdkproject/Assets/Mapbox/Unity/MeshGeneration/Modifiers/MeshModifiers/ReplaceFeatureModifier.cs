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
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Replace Feature Modifier")]
	public class ReplaceFeatureModifier : GameObjectModifier, IReplacementCriteria
	{

		private List<Vector2d> _latLonToSpawn;

		private Dictionary<ulong, GameObject> _objects;
		private Dictionary<ulong, Vector2d> _objectPosition;
		private static GameObject _poolGameObject;
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

		private static AbstractMap _abstractMap;

		public SpawnPrefabOptions SpawnPrefabOptions
		{
			set
			{
				_options = value;
			}
		}

		public List<string> PrefabLocations
		{
			set
			{
				_prefabLocations = value;
			}
		}

		public List<string> BlockedIds
		{
			set
			{
				_explicitlyBlockedFeatureIds = value;
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			//duplicate the list of lat/lons to track which coordinates have already been spawned

			_featureId = new List<List<string>>();

			for (int i = 0; i < _prefabLocations.Count; i++)
			{
				_featureId.Add(new List<string>());
			}
			if (_objects == null)
			{
				_objects = new Dictionary<ulong, GameObject>();
				_objectPosition = new Dictionary<ulong, Vector2d>();
				if(_poolGameObject == null)
				{
					_poolGameObject = new GameObject("_inactive_prefabs_pool");
				}
				if(_abstractMap == null)
				{
					_abstractMap = FindObjectOfType<AbstractMap>();
				}
				if(_abstractMap != null)
				{
					_poolGameObject.transform.SetParent(_abstractMap.transform, true);
				}
			}
			_latLonToSpawn = new List<Vector2d>();
			foreach (var loc in _prefabLocations)
			{
				_latLonToSpawn.Add(Conversions.StringToLatLon(loc));
			}
		}

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (SpawnPrefabOptions)properties;
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
						string idCandidate = (_tempFeatureId.Length <= 3) ? _tempFeatureId : _tempFeatureId.Substring(0, _tempFeatureId.Length - 3);
						_featureId[index].Add(idCandidate);
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		/// <summary>
		/// Check the feature against the list of lat/lons in the modifier
		/// </summary>
		/// <returns><c>true</c>, if the feature overlaps with a lat/lon in the modifier <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		public bool ShouldReplaceFeature(VectorFeatureUnity feature)
		{
			int index = -1;

			//preventing spawning of explicitly blocked features
			foreach (var blockedId in _explicitlyBlockedFeatureIds)
			{
				if (feature.Data.Id.ToString() == blockedId)
				{
					return true;
				}
			}

			foreach (var point in _prefabLocations)
			{
				try
				{
					index++;
					if (_featureId[index] != null)
					{
						foreach (var featureId in _featureId[index])
						{
							var latlngVector = Conversions.StringToLatLon(point);
							var from = Conversions.LatLonToMeters(latlngVector.x, latlngVector.y);
							var to = new Vector2d((feature.Points[0][0].x / feature.Tile.TileScale) + feature.Tile.Rect.Center.x, (feature.Points[0][0].z / feature.Tile.TileScale) + feature.Tile.Rect.Center.y);
							var dist = Vector2d.Distance(from, to);
							if (dist > 500)
							{
								return false;
							}
							if (feature.Data.Id.ToString().StartsWith(featureId, StringComparison.CurrentCulture))
							{
								return true;
							}
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}

			}
			return false;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			//replace the feature only once per lat/lon
			Vector2d latLong = Vector2d.zero;
			if (ShouldSpawnFeature(ve.Feature, out latLong))
			{
				SpawnPrefab(ve, tile, latLong);
			}
		}

		private void SpawnPrefab(VectorEntity ve, UnityTile tile, Vector2d latLong)
		{
			GameObject go;

			var featureId = ve.Feature.Data.Id;
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
				_objectPosition.Add(featureId, latLong);
				go.transform.SetParent(ve.GameObject.transform, false);
			}

			PositionScaleRectTransform(ve, tile, go, latLong);

			if (_options.AllPrefabsInstatiated != null)
			{
				_options.AllPrefabsInstatiated(_prefabList);
			}
		}

		public void PositionScaleRectTransform(VectorEntity ve, UnityTile tile, GameObject go, Vector2d latLong)
		{
			go.transform.localScale = _options.prefab.transform.localScale;
			RectTransform goRectTransform;
			IFeaturePropertySettable settable = null;
			var latLongPosition = new Vector3();
			var centroidVector = new Vector3();
			foreach (var point in ve.Feature.Points[0])
			{
				centroidVector += point;
			}
			centroidVector = centroidVector / ve.Feature.Points[0].Count;

			latLongPosition = Conversions.LatitudeLongitudeToUnityTilePosition(latLong, tile.CurrentZoom, tile.TileScale, 4096).ToVector3xz();
			latLongPosition.y = centroidVector.y;

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

			settable = go.GetComponent<IFeaturePropertySettable>();
			if (settable != null)
			{
				settable.Set(ve.Feature.Properties);
			}

			if (_options.scaleDownWithWorld)
			{
				go.transform.localScale = (go.transform.localScale * (tile.TileScale));
			}
		}

		/// <summary>
		/// Checks if the feature should be used to spawn a prefab, once per lat/lon
		/// </summary>
		/// <returns><c>true</c>, if the feature should be spawned <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		private bool ShouldSpawnFeature(VectorFeatureUnity feature, out Vector2d latLong)
		{
			latLong = Vector2d.zero;
			if (feature == null)
			{
				return false;
			}

			if (_objects.ContainsKey(feature.Data.Id))
			{
				_objectPosition.TryGetValue(feature.Data.Id, out latLong);
				_latLonToSpawn.Remove(latLong);
				return true;
			}

			foreach (var point in _latLonToSpawn)
			{
				if (feature.ContainsLatLon(point))
				{
					_latLonToSpawn.Remove(point);
					latLong = point;
					return true;
				}
			}

			return false;
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

		public override void Clear()
		{
			foreach (var gameObject in _objects.Values)
			{
				gameObject.Destroy();
			}
			_objects.Clear();
			_objectPosition.Clear();
			_poolGameObject.Destroy();
		}
	}
}
