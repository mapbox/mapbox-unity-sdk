using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.MeshGeneration.Modifiers;

namespace Mapbox.Unity.MeshGeneration.Interfaces
{
	using Mapbox.VectorTile;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;

	/// <summary>
	/// Layer visualizers contains sytling logic and processes features
	/// </summary>
	public abstract class LayerVisualizerBase : ScriptableObject
	{
		public abstract bool Active { get; }
		public abstract string Key { get; set; }
		public abstract VectorSubLayerProperties SubLayerProperties { get; set; }

		protected LayerPerformanceOptions _performanceOptions;
		protected VectorSubLayerProperties _sublayerProperties;
		protected ModifierStackBase _defaultStack;
		protected List<ModifierStack> _modifierStacks;
		protected HashSet<ModifierBase> _coreModifiers = new HashSet<ModifierBase>();

		public abstract void Create(UnityTile tile, Action<UnityTile, LayerVisualizerBase> callback = null);

		public event System.EventHandler LayerVisualizerHasChanged;

		public virtual void Initialize()
		{

		}

		public virtual void SetProperties(VectorSubLayerProperties properties)
		{
			_coreModifiers = new HashSet<ModifierBase>();

			if (_sublayerProperties == null && properties != null)
			{
				_sublayerProperties = properties;
				if (_performanceOptions == null && properties.performanceOptions != null)
				{
					_performanceOptions = properties.performanceOptions;
				}
			}

			_modifierStacks = new List<ModifierStack>();
			foreach (var modifierStack in _sublayerProperties.ModifierStacks)
			{
				_modifierStacks.Add(modifierStack);
			}

			if (_defaultStack == null)
			{
				_defaultStack = ScriptableObject.CreateInstance<ModifierStack>();
				((ModifierStack)_defaultStack).moveFeaturePositionTo = _sublayerProperties.moveFeaturePositionTo;
			}
			else
			{
				// HACK - to clean out the Modifiers.
				// Will this trigger GC that we could avoid ??
				_defaultStack.MeshModifiers.Clear();
				_defaultStack.GoModifiers.Clear();
			}


			//Add any additional modifiers that were added.
			if (_defaultStack.MeshModifiers == null)
			{
				_defaultStack.MeshModifiers = new List<MeshModifier>();
			}
			if (_defaultStack.GoModifiers == null)
			{
				_defaultStack.GoModifiers = new List<GameObjectModifier>();
			}

			// Setup material options.
			_sublayerProperties.materialOptions.SetDefaultMaterialOptions();

			switch (_sublayerProperties.coreOptions.geometryType)
			{
				case VectorPrimitiveType.Point:
				case VectorPrimitiveType.Custom:
					{
						// Let the user add anything that they want
						if (_sublayerProperties.coreOptions.snapToTerrain == true)
						{
							//AddOrCreateMeshModifier<SnapTerrainModifier>();
						}

						break;
					}
				case VectorPrimitiveType.Line:
					{
						if (_sublayerProperties.coreOptions.snapToTerrain == true)
						{
							AddOrCreateMeshModifier<SnapTerrainModifier>();
						}

						var lineMeshMod = AddOrCreateMeshModifier<LineMeshForPolygonsModifier>();
						lineMeshMod.SetProperties(_sublayerProperties.lineGeometryOptions);
						lineMeshMod.ModifierHasChanged += UpdateVector;

						if (_sublayerProperties.extrusionOptions.extrusionType != Map.ExtrusionType.None)
						{
							var heightMod = AddOrCreateMeshModifier<HeightModifier>();
							heightMod.SetProperties(_sublayerProperties.extrusionOptions);
							heightMod.ModifierHasChanged += UpdateVector;
						}
						else
						{
							_sublayerProperties.extrusionOptions.PropertyHasChanged += UpdateVector;
						}

						//collider modifier options
						var lineColliderMod = AddOrCreateGameObjectModifier<ColliderModifier>();
						lineColliderMod.SetProperties(_sublayerProperties.colliderOptions);
						lineColliderMod.ModifierHasChanged += UpdateVector;

						var lineStyleMod = AddOrCreateGameObjectModifier<MaterialModifier>();
						lineStyleMod.SetProperties(_sublayerProperties.materialOptions);
						lineStyleMod.ModifierHasChanged += UpdateVector;

						break;
					}
				case VectorPrimitiveType.Polygon:
					{
						if (_sublayerProperties.coreOptions.snapToTerrain == true)
						{
							AddOrCreateMeshModifier<SnapTerrainModifier>();
						}

						var poly = AddOrCreateMeshModifier<PolygonMeshModifier>();

						UVModifierOptions uvModOptions = new UVModifierOptions();
						uvModOptions.texturingType = (_sublayerProperties.materialOptions.style == StyleTypes.Custom) ? _sublayerProperties.materialOptions.customStyleOptions.texturingType : _sublayerProperties.materialOptions.texturingType;
						uvModOptions.atlasInfo = (_sublayerProperties.materialOptions.style == StyleTypes.Custom) ? _sublayerProperties.materialOptions.customStyleOptions.atlasInfo : _sublayerProperties.materialOptions.atlasInfo;
						uvModOptions.style = _sublayerProperties.materialOptions.style;
						poly.SetProperties(uvModOptions);

						if (_sublayerProperties.extrusionOptions.extrusionType != Map.ExtrusionType.None)
						{
							//replace materialOptions with styleOptions
							bool useTextureSideWallModifier =
							(_sublayerProperties.materialOptions.style == StyleTypes.Custom) ?
								(_sublayerProperties.materialOptions.customStyleOptions.texturingType == UvMapType.Atlas || _sublayerProperties.materialOptions.customStyleOptions.texturingType == UvMapType.AtlasWithColorPalette)
								: (_sublayerProperties.materialOptions.texturingType == UvMapType.Atlas || _sublayerProperties.materialOptions.texturingType == UvMapType.AtlasWithColorPalette);

							if (useTextureSideWallModifier)
							{
								var atlasMod = AddOrCreateMeshModifier<TextureSideWallModifier>();
								GeometryExtrusionWithAtlasOptions atlasOptions = new GeometryExtrusionWithAtlasOptions(_sublayerProperties.extrusionOptions, uvModOptions);
								atlasMod.SetProperties(atlasOptions);
								_sublayerProperties.extrusionOptions.PropertyHasChanged += UpdateVector;
							}
							else
							{
								var heightMod = AddOrCreateMeshModifier<HeightModifier>();
								heightMod.SetProperties(_sublayerProperties.extrusionOptions);
								heightMod.ModifierHasChanged += UpdateVector;
							}
						}
						else
						{
							_sublayerProperties.extrusionOptions.PropertyHasChanged += UpdateVector;
						}

						//collider modifier options
						var polyColliderMod = AddOrCreateGameObjectModifier<ColliderModifier>();
						polyColliderMod.SetProperties(_sublayerProperties.colliderOptions);
						polyColliderMod.ModifierHasChanged += UpdateVector;

						var styleMod = AddOrCreateGameObjectModifier<MaterialModifier>();
						styleMod.SetProperties(_sublayerProperties.materialOptions);
						styleMod.ModifierHasChanged += UpdateVector;


						bool isCustomStyle = (_sublayerProperties.materialOptions.style == StyleTypes.Custom);
						if ((isCustomStyle) ? (_sublayerProperties.materialOptions.customStyleOptions.texturingType == UvMapType.AtlasWithColorPalette)
							: (_sublayerProperties.materialOptions.texturingType == UvMapType.AtlasWithColorPalette))
						{
							var colorPaletteMod = AddOrCreateGameObjectModifier<MapboxStylesColorModifier>();
							colorPaletteMod.m_scriptablePalette = (isCustomStyle) ? _sublayerProperties.materialOptions.customStyleOptions.colorPalette : _sublayerProperties.materialOptions.colorPalette;
							_sublayerProperties.materialOptions.PropertyHasChanged += UpdateVector;
							//TODO: Add SetProperties Method to MapboxStylesColorModifier
						}

						break;
					}
				default:
					break;
			}

			_sublayerProperties.coreOptions.PropertyHasChanged += UpdateVector;
			_sublayerProperties.filterOptions.PropertyHasChanged += UpdateVector;

			_sublayerProperties.filterOptions.RegisterFilters();
			if (_sublayerProperties.MeshModifiers != null)
			{
				_defaultStack.MeshModifiers.AddRange(_sublayerProperties.MeshModifiers);
			}
			if (_sublayerProperties.GoModifiers != null)
			{
				_defaultStack.GoModifiers.AddRange(_sublayerProperties.GoModifiers);
			}

			_sublayerProperties.PropertyHasChanged += UpdateVector;
		}

		public virtual void Clear()
		{

		}

		public void UnregisterTile(UnityTile tile)
		{
			OnUnregisterTile(tile);
		}

		public virtual void OnUnregisterTile(UnityTile tile)
		{

		}

		public virtual void UnbindSubLayerEvents()
		{
			foreach (var modifier in _defaultStack.MeshModifiers)
			{
				modifier.UnbindProperties();
				modifier.ModifierHasChanged -= UpdateVector;
			}
			foreach (var modifier in _defaultStack.GoModifiers)
			{
				modifier.UnbindProperties();
				modifier.ModifierHasChanged -= UpdateVector;
			}

			_sublayerProperties.extrusionOptions.PropertyHasChanged -= UpdateVector;
			_sublayerProperties.coreOptions.PropertyHasChanged -= UpdateVector;
			_sublayerProperties.filterOptions.PropertyHasChanged -= UpdateVector;
			_sublayerProperties.filterOptions.UnRegisterFilters();
			_sublayerProperties.materialOptions.PropertyHasChanged -= UpdateVector;

			_sublayerProperties.PropertyHasChanged -= UpdateVector;
		}

		protected virtual void UpdateVector(object sender, EventArgs e)
		{

		}

		protected virtual void OnUpdateLayerVisualizer(System.EventArgs e)
		{
			System.EventHandler handler = LayerVisualizerHasChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		protected T FindMeshModifier<T>() where T : MeshModifier
		{
			MeshModifier mod = _defaultStack.MeshModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			return (T)mod;
		}

		protected T FindGameObjectModifier<T>() where T : GameObjectModifier
		{
			GameObjectModifier mod = _defaultStack.GoModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			return (T)mod;
		}

		protected T AddOrCreateMeshModifier<T>() where T : MeshModifier
		{
			MeshModifier mod = _defaultStack.MeshModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			if (mod == null)
			{
				mod = (MeshModifier)CreateInstance(typeof(T));
				_coreModifiers.Add(mod);
				_defaultStack.MeshModifiers.Add(mod);
			}
			return (T)mod;
		}

		protected T AddOrCreateGameObjectModifier<T>() where T : GameObjectModifier
		{
			GameObjectModifier mod = _defaultStack.GoModifiers.FirstOrDefault(x => x.GetType() == typeof(T));
			if (mod == null)
			{
				mod = (GameObjectModifier)CreateInstance(typeof(T));
				_coreModifiers.Add(mod);
				_defaultStack.GoModifiers.Add(mod);
			}
			return (T)mod;
		}
	}
}
