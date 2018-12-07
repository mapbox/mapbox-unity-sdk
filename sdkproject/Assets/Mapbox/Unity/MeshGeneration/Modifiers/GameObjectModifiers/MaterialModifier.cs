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
			_options.PropertyHasChanged += UpdateModifier;
		}

		public override void UnbindProperties()
		{
			_options.PropertyHasChanged -= UpdateModifier;
		}

		private float GetRenderMode(float val)
		{
			return Mathf.Approximately(val, 1.0f) ? 0f : 3f;
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			var min = Math.Min(_options.materials.Length, ve.MeshFilter.sharedMesh.subMeshCount);
			var mats = new Material[min];

			if (_options.style == StyleTypes.Custom)
			{
				for (int i = 0; i < min; i++)
				{
					mats[i] = _options.customStyleOptions.materials[i].Materials[UnityEngine.Random.Range(0, _options.customStyleOptions.materials[i].Materials.Length)];
				}
			}
			else if (_options.style == StyleTypes.Satellite)
			{
				for (int i = 0; i < min; i++)
				{
					mats[i] = Instantiate(_options.materials[i].Materials[UnityEngine.Random.Range(0, _options.materials[i].Materials.Length)]);
				}

				mats[0].mainTexture = tile.GetRasterData();
				mats[0].mainTextureScale = new Vector2(1f, 1f);
			}
			else
			{
				float renderMode = 0.0f;
				switch (_options.style)
				{
					case StyleTypes.Light:
						renderMode = GetRenderMode(_options.lightStyleOpacity);
						break;
					case StyleTypes.Dark:
						renderMode = GetRenderMode(_options.darkStyleOpacity);
						break;
					case StyleTypes.Color:
						renderMode = GetRenderMode(_options.colorStyleColor.a);
						break;
					default:
						break;
				}
				for (int i = 0; i < min; i++)
				{
					mats[i] = _options.materials[i].Materials[UnityEngine.Random.Range(0, _options.materials[i].Materials.Length)];
					mats[i].SetFloat("_Mode", renderMode);
				}
			}
			ve.MeshRenderer.materials = mats;
		}

		public override void OnPoolItem(VectorEntity vectorEntity)
		{
			if (_options.style == StyleTypes.Satellite)
			{
				foreach (var material in vectorEntity.MeshRenderer.sharedMaterials)
				{
					DestroyImmediate(material, true);
				}
			}
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
