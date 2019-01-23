namespace Mapbox.Unity.Map
{
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	public static class PresetSubLayerPropertiesFetcher
	{
		/// <summary>
		/// Gets the default sub layer properties for the chosen preset type.
		/// </summary>
		/// <returns>The sub layer properties.</returns>
		/// <param name="type">Type.</param>
		public static VectorSubLayerProperties GetSubLayerProperties(PresetFeatureType type)
		{
			//CoreOptions properties
			VectorPrimitiveType geometryType = VectorPrimitiveType.Polygon;
			string layerName = "building";
			string sublayerName = "Untitled";

			//Line Geometry Options
			float lineWidth = 1.0f;

			//Geometry Extrusion Options
			ExtrusionType extrusionType = ExtrusionType.None;
			ExtrusionGeometryType extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
			string propertyName = "height";
			float extrusionScaleFactor = 1.0f;
			float extrusionHeight = 1.0f;

			//Filter Options
			LayerFilterCombinerOperationType combinerType = LayerFilterCombinerOperationType.Any;
			List<LayerFilter> filters = new List<LayerFilter>();


			// Material Options
			StyleTypes style = StyleTypes.Realistic;

			//Misc options
			bool buildingsWithUniqueIds = true;
			PositionTargetType positionTargetType = PositionTargetType.TileCenter;

			//Modifiers
			List<MeshModifier> meshModifiers = new List<MeshModifier>();
			List<GameObjectModifier> gameObjectModifiers = new List<GameObjectModifier>();
			ColliderType colliderType = ColliderType.None;

			switch (type)
			{
				case PresetFeatureType.Buildings:
					layerName = "building";
					geometryType = VectorPrimitiveType.Polygon;
					extrusionType = ExtrusionType.PropertyHeight;
					extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
					propertyName = "height";
					style = StyleTypes.Realistic;
					break;
				case PresetFeatureType.Roads:
					layerName = "road";
					geometryType = VectorPrimitiveType.Line;
					lineWidth = 1.0f;
					style = StyleTypes.Dark;
					extrusionType = ExtrusionType.AbsoluteHeight;
					extrusionGeometryType = ExtrusionGeometryType.RoofAndSide;
					break;
				case PresetFeatureType.Points:
					layerName = "poi_label";
					geometryType = VectorPrimitiveType.Point;
					break;
				case PresetFeatureType.Landuse:
					layerName = "landuse";
					geometryType = VectorPrimitiveType.Polygon;
					style = StyleTypes.Color;
					break;
				case PresetFeatureType.Custom:
					layerName = "";
					geometryType = VectorPrimitiveType.Custom;
					style = StyleTypes.Custom;
					break;
				default:
					break;
			}

			VectorSubLayerProperties _properties = new VectorSubLayerProperties();

			_properties.presetFeatureType = type;

			_properties.coreOptions = new CoreVectorLayerProperties
			{
				isActive = true,
				layerName = layerName,
				geometryType = geometryType,
				snapToTerrain = true,
				combineMeshes = false,
				sublayerName = sublayerName
			};

			_properties.lineGeometryOptions = new LineGeometryOptions
			{
				Width = lineWidth
			};

			_properties.extrusionOptions = new GeometryExtrusionOptions
			{
				extrusionType = extrusionType,
				extrusionGeometryType = extrusionGeometryType,
				propertyName = propertyName,
				extrusionScaleFactor = extrusionScaleFactor,
				maximumHeight = extrusionHeight
			};

			_properties.filterOptions = new VectorFilterOptions
			{
				combinerType = combinerType,
				filters = filters
			};

			_properties.materialOptions = new GeometryMaterialOptions
			{
				style = style,
				colorStyleColor = Constants.GUI.Colors.EDITOR_FEATURE_DEFAULT_COLOR
			};
			_properties.materialOptions.SetDefaultMaterialOptions();
			_properties.buildingsWithUniqueIds = buildingsWithUniqueIds;
			_properties.moveFeaturePositionTo = positionTargetType;
			_properties.MeshModifiers = meshModifiers;
			_properties.GoModifiers = gameObjectModifiers;
			_properties.colliderOptions = new ColliderOptions
			{
				colliderType = colliderType
			};

			return _properties;
		}

		/// <summary>
		/// Gets the default preset type from supplied layerName.
		/// </summary>
		/// <param name="layerName">Layer name.</param>
		public static PresetFeatureType GetPresetTypeFromLayerName(string layerName)
		{
			switch (layerName)
			{
				case "building":
					return PresetFeatureType.Buildings;
				case "road":
					return PresetFeatureType.Roads;
				case "landuse":
					return PresetFeatureType.Landuse;
				case "poi_label":
					return PresetFeatureType.Points;
				default:
					return PresetFeatureType.Custom;
			}
		}
	}
}