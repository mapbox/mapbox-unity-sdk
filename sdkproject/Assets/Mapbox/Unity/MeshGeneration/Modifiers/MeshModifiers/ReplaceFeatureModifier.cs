namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;
	using Mapbox.Utils;


	/// <summary>
	/// ReplaceBuildingFeatureModifier takes in POIs and checks if the feature layer has those points and deletes them
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Replace Feature Modifier")]
	public class ReplaceFeatureModifier : PrefabModifier, IReplacementCriteria
	{
		[SerializeField]
		private List<Vector2d> LatLon;

		public bool ShouldReplaceFeature( VectorFeatureUnity feature )
		{
			return true;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			if(ShouldReplaceFeature(ve.Feature))
			{
				base.Run(ve, tile);
			}
		}
	}
}
