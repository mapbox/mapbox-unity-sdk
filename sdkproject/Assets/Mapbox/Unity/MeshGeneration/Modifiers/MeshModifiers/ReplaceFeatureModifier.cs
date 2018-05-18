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
		private List<Vector2d> LatLon;
		private List<Vector2d> _latLonToSpawn;

		public override void Initialize()
		{
			base.Initialize();
			//duplicate the list of lat/lons to track which coordinates have already been spawned
			_latLonToSpawn = new List<Vector2d>(LatLon);
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
				if (feature.ContainsLatLon(point))
				{
					return true;
				}
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
				if (feature.ContainsLatLon(point))
				{
					_latLonToSpawn.Remove(point);
					return true;
				}
			}

			return false;
		}


	}
}
