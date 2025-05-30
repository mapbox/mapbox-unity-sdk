using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule;
using Mapbox.ImageModule.Terrain;
using Mapbox.ImageModule.Terrain.Settings;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using UnityEngine;

namespace Mapbox.Example.Scripts.ModuleBehaviours
{
	public class TerrainLayerModuleScript : ModuleConstructorScript
	{
		public TerrainLayerModuleSettings Settings = new TerrainLayerModuleSettings()
		{
			RejectTilesOutsideZoom = new Vector2(10, 25),
			DataSettings = new ImageSourceSettings()
			{
				ClampDataLevelToMax = 14
			}
		};

		public override ILayerModule ModuleImplementation { get; protected set; }

		//do not delete
		//having a start method forces unity to have an enable/disable script checkbox in the inspector
		//and even though we aren't using Start method, we are using that checkbox to enable/disable the module
		private void Start()
		{
			
		}

		public override ILayerModule ConstructModule(MapService service, IMapInformation mapInformation, UnityContext unityContext)
		{
			var elevationTileset = MapboxDefaultElevation.GetParameters(Settings.SourceType);
			Settings.DataSettings.TilesetId = elevationTileset.Id;
			
			var module =
				new TerrainLayerModule(
					service.GetTerrainRasterSource(Settings.DataSettings),
					Settings,
					new ElevatedTerrainStrategy());

			mapInformation.QueryElevation = module.QueryElevation;
			ModuleImplementation = module;
			return ModuleImplementation;
		}
	}
}