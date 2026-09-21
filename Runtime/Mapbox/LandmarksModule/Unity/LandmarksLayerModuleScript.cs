using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarksModule;
using UnityEngine;
using UnityEngine.Events;

namespace Mapbox.LandmarksModule.Unity
{
	public class LandmarksLayerModuleScript : ModuleConstructorScript
	{
		[Tooltip("Material every landmark mesh is rendered with. Must be a glTF-compatible material that exposes baseColorFactor - use the shipped LandmarkMaterialGLTF unless you have built your own from the same subgraphs, since landmark tiles supply PBR textures the standard URP shaders will not bind.")]
		public Material BaseMaterial;

		// Initialised from the const so a scene saved before this field existed keeps working -
		// TilesetId falls back to Constants.Map.LandmarksTilesetId at construction time whenever
		// the serialized value is null or empty, which also covers a value cleared in the Inspector.
		[Tooltip("3D tileset id landmark tiles are fetched from. Leave as the default to use Mapbox's own 3D buildings tileset; point it at your own tileset id to render custom 3D landmark data instead. An empty value falls back to the default tileset.")]
		public string TilesetId = Constants.Map.LandmarksTilesetId;

		public override ILayerModule ModuleImplementation { get; protected set; }

		public override ILayerModule ConstructModule(MapService service, IMapInformation mapInformation, UnityContext unityContext)
		{
			if (BaseMaterial == null)
			{
				// Logged rather than refused: ModuleConstructorScript.ConstructModule's callers
				// (MapboxMapBehaviour, LoggingMapBehaviour, CompositeLayerModuleScript) add the
				// return value straight into MapboxMapVisualizer.LayerModules and iterate it
				// unconditionally, so returning null here would turn a missing material into an
				// NRE somewhere else instead of a clear error at the source.
				Debug.LogError($"{nameof(LandmarksLayerModuleScript)} has no {nameof(BaseMaterial)} assigned, so no landmarks will be drawn. Assign the shipped LandmarkMaterialGLTF material (Runtime/Mapbox/LandmarksModule/Materials/LandmarkMaterialGLTF.mat) in the Inspector.", this);
			}

			var module = GetLayerModule(unityContext, mapInformation, service);

			ModuleImplementation = module;
			return ModuleImplementation;
		}

		private LandmarksLayerModule GetLayerModule(UnityContext unityContext, IMapInformation mapInformation, MapService service)
		{
			var tilesetId = string.IsNullOrEmpty(TilesetId) ? Constants.Map.LandmarksTilesetId : TilesetId;
			var model = new LandmarksLayerModule(unityContext, mapInformation, service, tilesetId, BaseMaterial);
			return model;
		}
	}
}
