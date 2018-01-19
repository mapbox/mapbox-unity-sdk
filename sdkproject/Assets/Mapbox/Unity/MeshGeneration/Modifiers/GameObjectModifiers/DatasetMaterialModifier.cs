namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using System;
	using Mapbox.Unity.MeshGeneration.Data;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Dataset Material Modifier")]
	public class DatasetMaterialModifier : GameObjectModifier
	{
		
		[SerializeField]
		private Material _materialTemplate;
		[SerializeField]
		private string _propertyName;
		[SerializeField]
		private int max = 14500;

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			VectorFeatureUnity feature = ve.Feature;
			float hue, saturation, value;
			Color baseColor = new Color(0.3f, 0, 1);
			Color.RGBToHSV(baseColor, out hue, out saturation, out value);

			if (feature.Properties.ContainsKey(_propertyName))
			{
				Material _material = new Material(_materialTemplate);
				object rampValue;
				feature.Properties.TryGetValue(_propertyName, out rampValue);
				float ramp = Convert.ToSingle(rampValue) / max;

				Color newColor = Color.HSVToRGB(hue,1-ramp,value);
				_material.color = newColor;

				var min = ve.MeshFilter.mesh.subMeshCount;
				var mats = new Material[min];
				for (int i = 0; i < min; i++)
				{

					mats[i] = _material;
				}
				ve.MeshRenderer.materials = mats;

			}

		}
	}
}