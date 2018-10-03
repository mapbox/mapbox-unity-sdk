namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using UnityEngine;


	[Serializable]
	public class CoreVectorLayerProperties : MapboxDataProperty, ISubLayerCoreOptions
	{
		[SerializeField]
		private string sourceId;
		[Tooltip("Is visualizer active.")]
		public bool isActive = true;
		[Tooltip("Name of the visualizer. ")]
		public string sublayerName = "untitled";
		[Tooltip("Primitive geometry type of the visualizer, allowed primitives - point, line, polygon")]
		public VectorPrimitiveType geometryType = VectorPrimitiveType.Polygon;
		[Tooltip("Name of the layer in the source tileset. This property is case sensitive.")]
		public string layerName = "layerName";
		[Tooltip("Snap features to the terrain elevation, use this option to draw features above terrain. ")]
		public bool snapToTerrain = true;
		[Tooltip("Groups features into one Unity GameObject.")]
		public bool combineMeshes = false;


		public override bool HasChanged
		{
			set
			{
				if (value == true)
				{
					OnPropertyHasChanged(new VectorLayerUpdateArgs { property = this });
				}
			}
		}

		/// <summary>
		/// Change the primtive type of the feature which will be used to decide
		/// what type of mesh operations features will require.
		/// In example, roads are generally visualized as lines and buildings are
		/// generally visualized as polygons.
		/// </summary>
		/// <param name="type">Primitive type of the featues in the layer.</param>
		public virtual void SetPrimitiveType(VectorPrimitiveType type)
		{
			if (geometryType != type)
			{
				geometryType = type;
				HasChanged = true;
			}
		}

	}
}
