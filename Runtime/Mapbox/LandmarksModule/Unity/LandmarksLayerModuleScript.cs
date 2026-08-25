using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarkModule;
using UnityEngine;
using UnityEngine.Events;

namespace Mapbox.LandmarkModule.Unity
{
	public class LandmarksLayerModuleScript : ModuleConstructorScript
	{
		public Material BaseMaterial;

		public override ILayerModule ModuleImplementation { get; protected set; }

		public override ILayerModule ConstructModule(MapService service, IMapInformation mapInformation, UnityContext unityContext)
		{
			var module = GetLayerModule(unityContext, mapInformation, service);

			ModuleImplementation = module;
			return ModuleImplementation;
		}

		private LandmarksLayerModule GetLayerModule(UnityContext unityContext, IMapInformation mapInformation, MapService service)
		{
			var model = new LandmarksLayerModule(unityContext, mapInformation, service, Constants.Map.LandmarksTilesetId, BaseMaterial);
			return model;
		}
	}
}
