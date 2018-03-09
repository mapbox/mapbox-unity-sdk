namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.VectorTile.ExtensionMethods;
	using Mapbox.Editor.NodeEditor;

	[CustomPropertyDrawer(typeof(CameraBoundsTileProviderOptions))]
	public class CameraBoundsTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var camera = property.FindPropertyRelative("camera");
			var updateInterval = property.FindPropertyRelative("updateInterval");
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), camera, new GUIContent { text = camera.displayName, tooltip = "Camera to control map extent." });
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), updateInterval, new GUIContent { text = updateInterval.displayName, tooltip = "Time in ms between map extent update." });
			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(RangeTileProviderOptions))]
	public class RangeTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				EditorGUI.PropertyField(position, subproperty, true);
				position.height = lineHeight;
				position.y += lineHeight;
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(RangeAroundTransformTileProviderOptions))]
	public class RangeAroundTransformTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				EditorGUI.PropertyField(position, subproperty, true);
				position.height = lineHeight;
				position.y += lineHeight;
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(MapLocationOptions))]
	public class MapLocationOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				EditorGUI.PropertyField(position, subproperty, true);
				position.height = lineHeight;
				position.y += lineHeight;
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return (2.0f * lineHeight);
		}
	}

	[CustomPropertyDrawer(typeof(MapPlacementOptions))]
	public class MapPlacementOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var placementType = property.FindPropertyRelative("placementType");
			var snapMapToTerrain = property.FindPropertyRelative("snapMapToZero");
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), placementType, new GUIContent { text = placementType.displayName, tooltip = EnumExtensions.Description((MapPlacementType)placementType.enumValueIndex) });
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), snapMapToTerrain, new GUIContent { text = snapMapToTerrain.displayName, tooltip = "If checked, map's root will be snapped to zero. " });
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return 2.0f * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(MapScalingOptions))]
	public class MapScalingOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var scalingType = property.FindPropertyRelative("scalingType");

			var conversionFactor = EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight),
														   scalingType,
														   new GUIContent
														   {
															   text = scalingType.displayName,
															   tooltip = EnumExtensions.Description((MapScalingType)scalingType.enumValueIndex)
														   });
			if ((MapScalingType)scalingType.enumValueIndex == MapScalingType.Custom)
			{
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("unitType"), true);
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("unityToMercatorConversionFactor"), new GUIContent { text = "Unity to Mercator   1 : ", tooltip = "TODO : FIX DESCRIPTION - Unity Tile Size " });
			}
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			var scalingType = property.FindPropertyRelative("scalingType");
			if ((MapScalingType)scalingType.enumValueIndex == MapScalingType.Custom)
			{
				return 3.0f * lineHeight;
			}
			else
			{
				return 1.0f * lineHeight;
			}
		}
	}

	[CustomPropertyDrawer(typeof(ElevationRequiredOptions))]
	public class ElevationRequiredOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("baseMaterial"));
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("exaggerationFactor"));
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("addCollider"));

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return 3.0f * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(ElevationModificationOptions))]
	public class ElevationModificationOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("sampleCount"));
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("useRelativeHeight"));

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return 2.0f * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(TerrainSideWallOptions))]
	public class TerrainSideWallOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var isSidewallActiveProp = property.FindPropertyRelative("isActive");
			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), isSidewallActiveProp, new GUIContent("Show Sidewalls"));
			if (isSidewallActiveProp.boolValue == true)
			{
				EditorGUI.indentLevel++;
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("wallHeight"));
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("wallMaterial"));
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			var isSidewallActiveProp = property.FindPropertyRelative("isActive");
			if (isSidewallActiveProp.boolValue == true)
			{
				return 3.0f * lineHeight;
			}
			return 1.0f * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(UnityLayerOptions))]
	public class UnityLayerOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var addtoLayerProp = property.FindPropertyRelative("addToLayer");
			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), addtoLayerProp, new GUIContent { text = "Add to Unity layer" });
			if (addtoLayerProp.boolValue == true)
			{
				EditorGUI.indentLevel++;
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("layerId"));
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			var addtoLayerProp = property.FindPropertyRelative("addToLayer");
			if (addtoLayerProp.boolValue == true)
			{
				return 2.0f * lineHeight;
			}
			return 1.0f * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(GeometryExtrusionOptions))]
	public class GeometryExtrusionOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//position.y = lineHeight;

			//showPosition = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), showPosition, label.text);
			//position.y += lineHeight;
			//EditorGUI.indentLevel++;
			//if (showPosition)
			{
				var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Extrusion Type"));
				var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");

				extrusionTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, extrusionTypeProperty.enumValueIndex, extrusionTypeProperty.enumDisplayNames);
				var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

				var minHeightProperty = property.FindPropertyRelative("minimumHeight");
				var maxHeightProperty = property.FindPropertyRelative("maximumHeight");

				EditorGUI.indentLevel++;
				switch (sourceTypeValue)
				{
					case Unity.Map.ExtrusionType.None:
						break;
					case Unity.Map.ExtrusionType.PropertyHeight:
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
						break;
					case Unity.Map.ExtrusionType.MinHeight:
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), minHeightProperty);
						//maxHeightProperty.floatValue = minHeightProperty.floatValue;
						break;
					case Unity.Map.ExtrusionType.MaxHeight:
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty);
						//min.floatValue = minHeightProperty.floatValue;
						break;
					case Unity.Map.ExtrusionType.RangeHeight:
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), minHeightProperty);
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty);
						break;
					case Unity.Map.ExtrusionType.AbsoluteHeight:
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extrusionGeometryType"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("propertyName"));
						position.y += lineHeight;
						EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), maxHeightProperty, new GUIContent { text = "Height" });
						break;
					default:
						break;
				}
				EditorGUI.indentLevel--;
			}
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");
			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			int rows = 0;
			//if (showPosition)
			{
				switch (sourceTypeValue)
				{
					case Unity.Map.ExtrusionType.None:
						rows += 1;
						break;
					case Unity.Map.ExtrusionType.PropertyHeight:
						rows += 3;
						break;
					case Unity.Map.ExtrusionType.MinHeight:
					case Unity.Map.ExtrusionType.MaxHeight:
					case Unity.Map.ExtrusionType.AbsoluteHeight:
						rows += 4;
						break;
					case Unity.Map.ExtrusionType.RangeHeight:
						rows += 5;
						break;
					default:
						rows += 2;
						break;
				}
			}
			return (float)rows * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(MaterialList))]
	public class MaterialListDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//EditorGUI.indentLevel++;
			position.y += lineHeight;
			var matArray = property.FindPropertyRelative("Materials");
			if (matArray.arraySize == 0)
			{
				matArray.arraySize = 1;
			}
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("Materials").GetArrayElementAtIndex(0), label);
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			var matList = property.FindPropertyRelative("Materials");
			int rows = (matList.isExpanded) ? matList.arraySize : 1;
			return (float)rows * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(GeometryMaterialOptions))]
	public class GeometryMaterialOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			//showPosition = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), showPosition, label.text);
			EditorGUI.LabelField(new Rect(position.x, position.y, position.width, lineHeight), new GUIContent { text = "Material Options", tooltip = "Unity materials to be used for features. " });
			EditorGUI.indentLevel++;
			//if (showPosition)
			{
				position.y += lineHeight;
				var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Texturing Type", tooltip = "Use image texture from the Imagery source as texture for roofs. " });
				var texturingType = property.FindPropertyRelative("texturingType");
				EditorGUI.indentLevel--;
				texturingType.enumValueIndex = EditorGUI.Popup(typePosition, texturingType.enumValueIndex, texturingType.enumDisplayNames);
				EditorGUI.indentLevel++;

				var matList = property.FindPropertyRelative("materials");
				if (matList.arraySize == 0)
				{
					matList.arraySize = 2;
				}

				var roofMat = matList.GetArrayElementAtIndex(0);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), roofMat, new GUIContent { text = "Roof Material", tooltip = "Unity material to use for extruded roof/top mesh. " });
				position.y += EditorGUI.GetPropertyHeight(roofMat);

				var wallMat = matList.GetArrayElementAtIndex(1);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), wallMat, new GUIContent { text = "Wall Material", tooltip = "Unity material to use for extruded wall/side mesh. " });
				position.y += EditorGUI.GetPropertyHeight(wallMat);

				if ((UvMapType)texturingType.enumValueIndex == UvMapType.Atlas)
				{
					position.y += lineHeight;
					var atlasInfo = property.FindPropertyRelative("atlasInfo");
					EditorGUI.ObjectField(new Rect(position.x, position.y, position.width, lineHeight), atlasInfo, new GUIContent { text = "Altas Info", tooltip = "Atlas information scriptable object, this defines how the texture roof and wall texture atlases will be used.  " });
				}
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			float height = 0.0f;
			if (showPosition)
			{
				height += (2.0f * lineHeight);
				var matList = property.FindPropertyRelative("materials");

				for (int i = 0; i < matList.arraySize; i++)
				{
					var matInList = matList.GetArrayElementAtIndex(i);
					height += EditorGUI.GetPropertyHeight(matInList);
				}
				var texturingType = property.FindPropertyRelative("texturingType");
				if ((UvMapType)texturingType.enumValueIndex == UvMapType.Atlas)
				{
					height += lineHeight;
				}
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}
			return height;
		}
	}

	[CustomPropertyDrawer(typeof(GeometryStylingOptions))]
	public class GeometryStylingOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		SerializedProperty isActiveProperty;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			isActiveProperty = property.FindPropertyRelative("isExtruded");

			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.PropertyField(position, property.FindPropertyRelative("extrusionOptions"), false);
			position.y += (EditorGUI.GetPropertyHeight(property.FindPropertyRelative("extrusionOptions")));
			//}
			//position.y += lineHeight;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("materialOptions"), false);
			EditorGUI.EndProperty();

		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (showPosition)
			{
				height += (2.0f * EditorGUIUtility.singleLineHeight);
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("extrusionOptions"), false);
				height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("materialOptions"), false);
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}

			return height;
		}
	}

	[CustomPropertyDrawer(typeof(VectorFilterOptions))]
	public class VectorFilterOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showFilters = true;

		GUIContent operatorGui = new GUIContent { text = "Operator", tooltip = "Filter operator to apply. " };
		GUIContent numValueGui = new GUIContent { text = "Num Value", tooltip = "Numeric value to match using the operator.  " };
		GUIContent strValueGui = new GUIContent { text = "Str Value", tooltip = "String value to match using the operator.  " };
		GUIContent minValueGui = new GUIContent { text = "Min", tooltip = "Minimum numeric value to match using the operator.  " };
		GUIContent maxValueGui = new GUIContent { text = "Max", tooltip = "Maximum numeric value to match using the operator.  " };

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;

			showFilters = EditorGUI.Foldout(position, showFilters, new GUIContent { text = "Filters", tooltip = "Filter features in a vector layer based on criterion specified.  " });
			if (showFilters)
			{
				var propertyFilters = property.FindPropertyRelative("filters");

				for (int i = 0; i < propertyFilters.arraySize; i++)
				{
					DrawLayerFilter(propertyFilters, i);
				}
				if (propertyFilters.arraySize > 0)
				{
					EditorGUILayout.PropertyField(property.FindPropertyRelative("combinerType"));
				}

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibutton"))
				{
					propertyFilters.arraySize++;
					//propertyFilters.GetArrayElementAtIndex(propertyFilters.arraySize - 1) = null;
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}

		void DrawLayerFilter(SerializedProperty propertyFilters, int index)
		{
			var property = propertyFilters.GetArrayElementAtIndex(index);
			var filterOperatorProp = property.FindPropertyRelative("filterOperator");

			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField(new GUIContent { text = "Key", tooltip = "Name of the property to use as key. This property is case sensitive." }, GUILayout.MaxWidth(150));


			switch ((LayerFilterOperationType)filterOperatorProp.enumValueIndex)
			{
				case LayerFilterOperationType.IsEqual:
				case LayerFilterOperationType.IsGreater:
				case LayerFilterOperationType.IsLess:
					EditorGUILayout.LabelField(operatorGui, GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField(numValueGui, GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.Contains:
					EditorGUILayout.LabelField(operatorGui, GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField(strValueGui, GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.IsInRange:
					EditorGUILayout.LabelField(operatorGui, GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField(minValueGui, GUILayout.MaxWidth(100));
					EditorGUILayout.LabelField(maxValueGui, GUILayout.MaxWidth(100));
					break;
				default:
					break;
			}

			EditorGUILayout.EndHorizontal();


			EditorGUILayout.BeginHorizontal();
			property.FindPropertyRelative("Key").stringValue = EditorGUILayout.TextField(property.FindPropertyRelative("Key").stringValue, GUILayout.MaxWidth(150));
			filterOperatorProp.enumValueIndex = EditorGUILayout.Popup(filterOperatorProp.enumValueIndex, filterOperatorProp.enumDisplayNames, GUILayout.MaxWidth(150));

			switch ((LayerFilterOperationType)filterOperatorProp.enumValueIndex)
			{
				case LayerFilterOperationType.IsEqual:
				case LayerFilterOperationType.IsGreater:
				case LayerFilterOperationType.IsLess:
					property.FindPropertyRelative("Min").doubleValue = EditorGUILayout.DoubleField(property.FindPropertyRelative("Min").doubleValue, GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.Contains:
					property.FindPropertyRelative("PropertyValue").stringValue = EditorGUILayout.TextField(property.FindPropertyRelative("PropertyValue").stringValue, GUILayout.MaxWidth(150));
					break;
				case LayerFilterOperationType.IsInRange:
					property.FindPropertyRelative("Min").doubleValue = EditorGUILayout.DoubleField(property.FindPropertyRelative("Min").doubleValue, GUILayout.MaxWidth(100));
					property.FindPropertyRelative("Max").doubleValue = EditorGUILayout.DoubleField(property.FindPropertyRelative("Max").doubleValue, GUILayout.MaxWidth(100));
					break;
				default:
					break;
			}
			if (GUILayout.Button(new GUIContent(" X "), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
			{
				propertyFilters.DeleteArrayElementAtIndex(index);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

		}
	}

	[CustomPropertyDrawer(typeof(CoreVectorLayerProperties))]
	public class CoreVectorLayerPropertiesDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		bool showFilters = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			//showPosition = EditorGUI.Foldout(position, showPosition, label.text);

			//if (showPosition)
			{

				//position.y += lineHeight;
				// Draw label.
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("isActive"));
				position.y += lineHeight;
				var primitiveType = property.FindPropertyRelative("geometryType");
				var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent { text = "Primitive Type", tooltip = "Primitive geometry type of the visualizer , allowed primitives - point, line, polygon." });

				primitiveType.enumValueIndex = EditorGUI.Popup(typePosition, primitiveType.enumValueIndex, primitiveType.enumDisplayNames);

				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("layerName"));

				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("snapToTerrain"));

				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("groupFeatures"));

				if ((VectorPrimitiveType)primitiveType.enumValueIndex == VectorPrimitiveType.Line)
				{
					position.y += lineHeight;
					EditorGUI.PropertyField(position, property.FindPropertyRelative("lineWidth"));
				}

			}
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var sourceTypeProperty = property.FindPropertyRelative("geometryType");

			float height = 0.0f;
			height += (((((VectorPrimitiveType)sourceTypeProperty.enumValueIndex == VectorPrimitiveType.Line)) ? 6.0f : 5.0f) * EditorGUIUtility.singleLineHeight);

			return height;
		}
	}

	[CustomPropertyDrawer(typeof(ImageryRasterOptions))]
	public class ImageryRasterOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			foreach (var item in property)
			{
				var subproperty = item as SerializedProperty;
				EditorGUI.PropertyField(position, subproperty, true);
				position.height = lineHeight;
				position.y += lineHeight;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int rows = (showPosition) ? 3 : 1;
			return (float)rows * lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(LayerSourceOptions))]
	public class LayerSourceOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("layerSource"), new GUIContent { tooltip = label.tooltip });
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(LayerPerformanceOptions))]
	public class LayerPerformanceOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		SerializedProperty isActiveProperty;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			isActiveProperty = property.FindPropertyRelative("isEnabled");

			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Enable Coroutines"));
			isActiveProperty.boolValue = EditorGUI.Toggle(typePosition, isActiveProperty.boolValue);

			if (isActiveProperty.boolValue == true)
			{
				EditorGUI.indentLevel++;
				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("entityPerCoroutine"), true);
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (isActiveProperty != null && isActiveProperty.boolValue == true)
			{
				height += (2.0f * EditorGUIUtility.singleLineHeight);
				//height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("layerSource"), false);
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}

			return height;
		}
	}
	[CustomPropertyDrawer(typeof(Style))]
	public class StyleOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position.height = lineHeight;

			EditorGUI.PropertyField(position, property.FindPropertyRelative("Id"), new GUIContent { text = "Map Id", tooltip = "Map Id corresponding to the tileset." });

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return lineHeight;
		}
	}

	//[CustomPropertyDrawer(typeof(TypeVisualizerTuple))]
	//public class TypeVisualizerBaseDrawer : PropertyDrawer
	//{
	//	static float lineHeight = EditorGUIUtility.singleLineHeight;
	//	bool showPosition = true;
	//	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	//	{
	//		EditorGUI.BeginProperty(position, label, property);

	//		position.height = lineHeight;

	//		EditorGUI.PropertyField(position, property.FindPropertyRelative("Stack"));

	//		EditorGUI.EndProperty();
	//	}
	//	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	//	{
	//		// Reserve space for the total visible properties.
	//		int rows = 2;
	//		//Debug.Log("Height - " + rows * lineHeight);
	//		return (float)rows * lineHeight;
	//	}
	//}

}