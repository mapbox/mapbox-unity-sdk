namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using System;
	/// <summary>
	/// Style Modifier is a basic modifier which uses data from GeometryMaterialOptions to set materials in VectorLayer renderers.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Style Modifier")]
	public class StyleModifier : GameObjectModifier
	{

		[SerializeField]
		GeometryMaterialOptions _options;

		public override void SetProperties(ModifierProperties properties)
		{
			_options = (GeometryMaterialOptions)properties;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			var min = Math.Min(_options.materials.Length, ve.MeshFilter.mesh.subMeshCount);
			var mats = new Material[min];

			if (_options.texturingType != UvMapType.Satellite)
			{
				for (int i = 0; i < min; i++)
				{
					mats[i] = _options.materials[i].Materials[UnityEngine.Random.Range(0, _options.materials[i].Materials.Length)];
				}
			}
			else
			{
				for (int i = 0; i < min; i++)
				{
					mats[i] = _options.materials[i].Materials[UnityEngine.Random.Range(0, _options.materials[i].Materials.Length)];
				}

				mats[0].mainTexture = tile.GetRasterData();
				mats[0].mainTextureScale = new Vector2(1f, 1f);
			}

			ve.MeshRenderer.materials = mats;
		}
	}
}
