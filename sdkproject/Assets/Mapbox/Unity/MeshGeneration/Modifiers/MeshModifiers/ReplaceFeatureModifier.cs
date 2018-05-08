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
	public class ReplaceFeatureModifier : MeshModifier
	{
		[SerializeField]
		private List<Vector2d> LatLon;

		private GeometryExtrusionOptions _options;
		public override ModifierType Type { get { return ModifierType.Preprocess; } }

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (GeometryExtrusionOptions)properties;
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, float scale)
		{
			Run(feature, md);
		}

		public override void Run(VectorFeatureUnity feature, MeshData md, UnityTile tile = null)
		{

		}

		protected virtual void GenerateWallMesh(MeshData md)
		{
		}
	}
}
