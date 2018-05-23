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
		private List<string> _latLonToSpawn;

		private Dictionary<GameObject, GameObject> _objects;
		[SerializeField]
		private SpawnPrefabOptions _options;
		private List<GameObject> _prefabList = new List<GameObject>();

		[SerializeField]
		[Geocode]
		private List<string> _prefabLocations;

		private string _featureId;

		public override void Initialize()
		{
			base.Initialize();
			//duplicate the list of lat/lons to track which coordinates have already been spawned
			_latLonToSpawn = new List<string>(_prefabLocations);
			_featureId = String.Empty;
			if (_objects == null)
			{
				_objects = new Dictionary<GameObject, GameObject>();
			}
			_latLonToSpawn = new List<string>(_prefabLocations);
		}

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (SpawnPrefabOptions)properties;
		}

		/// <summary>
		/// Check the feature against the list of lat/lons in the modifier
		/// </summary>
		/// <returns><c>true</c>, if the feature overlaps with a lat/lon in the modifier <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		public bool ShouldReplaceFeature( VectorFeatureUnity feature )
		{
			foreach( var point in _prefabLocations )
			{
				var coord = Conversions.StringToLatLon(point);
				if (feature.ContainsLatLon(coord))
				{
					

					if(feature.Data.Id != 0 && String.IsNullOrEmpty(_featureId))
					{
						_featureId = feature.Data.Id.ToString();
						_featureId = _featureId.Substring(0, _featureId.Length - 3);
					}
					return true;
				}
			}

			if(feature.Data.Id.ToString().StartsWith(_featureId, StringComparison.CurrentCulture) &&
			  !String.IsNullOrEmpty(_featureId))
			{
				return true;
			}

			return false;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			//replace the feature only once per lat/lon
			if(ShouldSpawnFeature(ve.Feature))
			{
				SpawnPrefab(ve, tile);
			}
		}

		private void SpawnPrefab(VectorEntity ve, UnityTile tile)
		{
			//int selpos = ve.Feature.Points[0].Count / 2;
			var met = new Vector3();
			foreach (var point in ve.Feature.Points[0])
			{
				met += point;
			}
			met = met / ve.Feature.Points[0].Count;

			RectTransform goRectTransform;
			IFeaturePropertySettable settable = null;
			GameObject go;

			if (_objects.ContainsKey(ve.GameObject))
			{
				go = _objects[ve.GameObject];
				settable = go.GetComponent<IFeaturePropertySettable>();
				if (settable != null)
				{
					go = (settable as MonoBehaviour).gameObject;
					settable.Set(ve.Feature.Properties);
				}
				// set gameObject transform
				go.name = ve.Feature.Data.Id.ToString();
				goRectTransform = go.GetComponent<RectTransform>();
				if (goRectTransform == null)
				{
					go.transform.localPosition = met;
				}
				else
				{
					goRectTransform.anchoredPosition3D = met;
				}
				//go.transform.localScale = Constants.Math.Vector3One;

				if (_options.scaleDownWithWorld)
				{
					go.transform.localScale = (go.transform.localScale * (tile.TileScale));
				}
				return;
			}
			else
			{
				go = Instantiate(_options.prefab);
				_prefabList.Add(go);
				_objects.Add(ve.GameObject, go);
			}

			go.name = ve.Feature.Data.Id.ToString();

			goRectTransform = go.GetComponent<RectTransform>();
			if (goRectTransform == null)
			{
				go.transform.localPosition = met;
			}
			else
			{
				goRectTransform.anchoredPosition3D = met;
			}
			go.transform.SetParent(ve.GameObject.transform, false);
			//go.transform.localScale = Constants.Math.Vector3One;

			settable = go.GetComponent<IFeaturePropertySettable>();
			if (settable != null)
			{
				settable.Set(ve.Feature.Properties);
			}

			if (_options.scaleDownWithWorld)
			{
				go.transform.localScale = (go.transform.localScale * (tile.TileScale));
			}

			if (_options.AllPrefabsInstatiated != null)
			{
				_options.AllPrefabsInstatiated(_prefabList);
			}
		}

		/// <summary>
		/// Checks if the feature should be used to spawn a prefab, once per lat/lon
		/// </summary>
		/// <returns><c>true</c>, if the feature should be spawned <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		private bool ShouldSpawnFeature(VectorFeatureUnity feature)
		{
			if (feature == null)
			{
				return false;
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
	}
}
