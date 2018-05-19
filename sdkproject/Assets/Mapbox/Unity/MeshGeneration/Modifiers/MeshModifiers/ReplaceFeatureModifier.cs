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


	/// <summary>
	/// ReplaceBuildingFeatureModifier takes in POIs and checks if the feature layer has those points and deletes them
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Replace Feature Modifier")]
	public class ReplaceFeatureModifier : PrefabModifier, IReplacementCriteria
	{
		[SerializeField]
		[Geocode]
		private List<string> LatLon;
		private List<string> _latLonToSpawn;

		private string _featureId;

		public override void Initialize()
		{
			base.Initialize();
			//duplicate the list of lat/lons to track which coordinates have already been spawned
			_latLonToSpawn = new List<string>(LatLon);
			_featureId = String.Empty;
		}

		/// <summary>
		/// Check the feature against the list of lat/lons in the modifier
		/// </summary>
		/// <returns><c>true</c>, if the feature overlaps with a lat/lon in the modifier <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		public bool ShouldReplaceFeature( VectorFeatureUnity feature )
		{
			foreach( var point in LatLon )
			{
				var coord = Conversions.StringToLatLon(point);
				if (feature.ContainsLatLon(coord))
				{

					//TODO: null check on feature.Data.Id
					if(String.IsNullOrEmpty(_featureId))
					{
						_featureId = feature.Data.Id.ToString();
						_featureId = _featureId.Substring(0, _featureId.Length - 3);
					}
					Debug.Log(_featureId);
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
				base.Run(ve, tile);
			}
		}

		/// <summary>
		/// Checks if the feature should be used to spawn a prefab, once per lat/lon
		/// </summary>
		/// <returns><c>true</c>, if the feature should be spawned <c>false</c> otherwise.</returns>
		/// <param name="feature">Feature.</param>
		private bool ShouldSpawnFeature(VectorFeatureUnity feature)
		{
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
