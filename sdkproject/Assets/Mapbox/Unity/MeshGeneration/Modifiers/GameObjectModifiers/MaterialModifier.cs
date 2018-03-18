namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Map;
	using System;

	/// <summary>
	/// Texture Modifier is a basic modifier which simply adds a TextureSelector script to the features.
	/// Logic is all pushed into this TextureSelector mono behaviour to make it's easier to change it in runtime.
	/// </summary>
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Material Modifier")]
	public class MaterialModifier : GameObjectModifier
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

			if (!_options.projectMapImagery)
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
					mats[i] = Instantiate(_options.materials[i].Materials[UnityEngine.Random.Range(0, _options.materials[i].Materials.Length)]);
				}

				mats[0].mainTexture = tile.GetRasterData();
				mats[0].mainTextureScale = new Vector2(1f, 1f);
			}

			ve.MeshRenderer.materials = mats;
		}
	}

	[Serializable]
	public class MaterialList
	{
		[SerializeField]
		public Material[] Materials;

		public MaterialList()
		{
			Materials = new Material[1];
		}
	}
}
