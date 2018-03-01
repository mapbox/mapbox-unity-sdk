namespace Mapbox.Editor
{
	using UnityEditor;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Interfaces;
	using Mapbox.Unity.MeshGeneration.Filters;
	using Mapbox.Editor.NodeEditor;

	[CustomPropertyDrawer(typeof(CameraBoundsTileProviderOptions))]
	public class CameraBoundsTileProviderOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("camera"));
			position.y += lineHeight;
			EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("updateInterval"));
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


	[CustomPropertyDrawer(typeof(MapPlacementOptions))]
	public class MapPlacementOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			showPosition = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), showPosition, label.text);
			position.y += lineHeight;
			//EditorGUI.indentLevel++;
			if (showPosition)
			{
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("placementType"), true);
				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("extentOptions"), true);
				//EditorGUI.indentLevel--;
			}
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			float height = 0.0f;
			if (showPosition)
			{
				height += (2.0f * lineHeight);
				var extentOptionsProp = property.FindPropertyRelative("extentOptions");
				height += EditorGUI.GetPropertyHeight(extentOptionsProp);
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}
			return height;
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

			showPosition = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), showPosition, label.text);
			position.y += lineHeight;
			//EditorGUI.indentLevel++;
			if (showPosition)
			{
				var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Extrusion Type"));
				var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");

				extrusionTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, extrusionTypeProperty.enumValueIndex, extrusionTypeProperty.enumDisplayNames);
				var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

				var minHeightProperty = property.FindPropertyRelative("minimumHeight");
				var maxHeightProperty = property.FindPropertyRelative("maximumHeight");

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
			}
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var extrusionTypeProperty = property.FindPropertyRelative("extrusionType");
			var sourceTypeValue = (Unity.Map.ExtrusionType)extrusionTypeProperty.enumValueIndex;

			int rows = 1;
			if (showPosition)
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
			showPosition = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, lineHeight), showPosition, label.text);
			//EditorGUI.indentLevel++;
			if (showPosition)
			{
				position.y += lineHeight;
				var projectMapImg = property.FindPropertyRelative("projectMapImagery");
				var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Project Imagery"));
				projectMapImg.boolValue = EditorGUI.Toggle(typePosition, projectMapImg.boolValue);

				//position.y += lineHeight;
				//EditorGUI.PropertyField(position, property.FindPropertyRelative("materials"));
				var matList = property.FindPropertyRelative("materials");
				if (matList.arraySize == 0)
				{
					matList.arraySize = 2;
				}

				var roofMat = matList.GetArrayElementAtIndex(0);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), roofMat, new GUIContent("Roof Material"));
				position.y += EditorGUI.GetPropertyHeight(roofMat);

				var wallMat = matList.GetArrayElementAtIndex(1);
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), wallMat, new GUIContent("Wall Material"));
				position.y += EditorGUI.GetPropertyHeight(wallMat);

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

	[CustomPropertyDrawer(typeof(LayerModifierOptions))]
	public class LayerModifierOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			showPosition = EditorGUI.Foldout(position, showPosition, label.text);
			//EditorGUI.indentLevel++;
			if (showPosition)
			{
				position.y += lineHeight;
				var typePosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Feature Position"));
				var featurePositionProperty = property.FindPropertyRelative("moveFeaturePositionTo");
				featurePositionProperty.enumValueIndex = EditorGUI.Popup(typePosition, featurePositionProperty.enumValueIndex, featurePositionProperty.enumDisplayNames);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Mesh Modifiers");

				var meshfac = property.FindPropertyRelative("MeshModifiers");

				for (int i = 0; i < meshfac.arraySize; i++)
				{
					var ind = i;
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
					//GUILayout.Space(5);
					meshfac.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(meshfac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(MeshModifier), false) as ScriptableObject;
					EditorGUILayout.EndVertical();
					if (GUILayout.Button(new GUIContent("+"), (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
					{
						ScriptableCreatorWindow.Open(typeof(MeshModifier), meshfac, ind);
					}
					if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
					{
						meshfac.DeleteArrayElementAtIndex(ind);
					}
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibuttonleft"))
				{
					meshfac.arraySize++;
					meshfac.GetArrayElementAtIndex(meshfac.arraySize - 1).objectReferenceValue = null;
				}
				if (GUILayout.Button(new GUIContent("Find Asset"), (GUIStyle)"minibuttonright"))
				{
					ScriptableCreatorWindow.Open(typeof(MeshModifier), meshfac);
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Game Object Modifiers");
				var gofac = property.FindPropertyRelative("GoModifiers");
				for (int i = 0; i < gofac.arraySize; i++)
				{
					var ind = i;
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
					GUILayout.Space(5);
					gofac.GetArrayElementAtIndex(ind).objectReferenceValue = EditorGUILayout.ObjectField(gofac.GetArrayElementAtIndex(i).objectReferenceValue, typeof(GameObjectModifier), false) as ScriptableObject;
					EditorGUILayout.EndVertical();

					if (GUILayout.Button(new GUIContent("+"), (GUIStyle)"minibuttonleft", GUILayout.Width(30)))
					{
						ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac, ind);
					}
					if (GUILayout.Button(new GUIContent("-"), (GUIStyle)"minibuttonright", GUILayout.Width(30)))
					{
						gofac.DeleteArrayElementAtIndex(ind);
					}
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibuttonleft"))
				{
					gofac.arraySize++;
					gofac.GetArrayElementAtIndex(gofac.arraySize - 1).objectReferenceValue = null;
				}
				if (GUILayout.Button(new GUIContent("Find Asset"), (GUIStyle)"minibuttonright"))
				{
					ScriptableCreatorWindow.Open(typeof(GameObjectModifier), gofac);
				}
				EditorGUILayout.EndHorizontal();
				//GUILayout.EndArea();
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (showPosition)
			{
				height += (2.0f * EditorGUIUtility.singleLineHeight);
			}
			else
			{
				height += (1.0f * EditorGUIUtility.singleLineHeight);
			}

			return height;
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
			showPosition = EditorGUI.Foldout(position, showPosition, label.text);

			if (showPosition)
			{

				position.y += lineHeight;
				// Draw label.
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("isActive"));
				position.y += lineHeight;
				var typePosition = EditorGUI.PrefixLabel(new Rect(position.x, position.y, position.width, lineHeight), GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Primitive Type"));
				var sourceTypeProperty = property.FindPropertyRelative("geometryType");
				sourceTypeProperty.enumValueIndex = EditorGUI.Popup(typePosition, sourceTypeProperty.enumValueIndex, sourceTypeProperty.enumDisplayNames);

				position.y += lineHeight;
				EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, lineHeight), property.FindPropertyRelative("layerName"));

				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("snapToTerrain"));

				position.y += lineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("groupFeatures"));

				position.y += lineHeight;
				showFilters = EditorGUI.Foldout(position, showFilters, "Filters");
				if (showFilters)
				{
					var propertyFilters = property.FindPropertyRelative("filters");

					for (int i = 0; i < propertyFilters.arraySize; i++)
					{
						DrawLayerFilter(propertyFilters, i);
					}
					EditorGUILayout.PropertyField(property.FindPropertyRelative("combinerType"));
					EditorGUILayout.BeginHorizontal();
					if (GUILayout.Button(new GUIContent("Add New Empty"), (GUIStyle)"minibuttonleft"))
					{
						propertyFilters.arraySize++;
						//facs.GetArrayElementAtIndex(facs.arraySize - 1).objectReferenceValue = null;
					}
					EditorGUILayout.EndHorizontal();
				}

			}
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = 0.0f;
			if (showPosition)
			{
				height += (7.0f * EditorGUIUtility.singleLineHeight);
			}
			else
			{
				height = EditorGUIUtility.singleLineHeight;
			}

			return height;
		}

		void DrawLayerFilter(SerializedProperty propertyFilters, int index)
		{
			var property = propertyFilters.GetArrayElementAtIndex(index);
			var filterOperatorProp = property.FindPropertyRelative("filterOperator");

			EditorGUILayout.BeginVertical();

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Key", GUILayout.MaxWidth(150));

			switch ((LayerFilterOperationType)filterOperatorProp.enumValueIndex)
			{
				case LayerFilterOperationType.IsEqual:
				case LayerFilterOperationType.IsGreater:
				case LayerFilterOperationType.IsLess:
					EditorGUILayout.LabelField("Operator", GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField("Num Value", GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.Contains:
					EditorGUILayout.LabelField("Operator", GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField("Str Value", GUILayout.MaxWidth(100));
					break;
				case LayerFilterOperationType.IsInRange:
					EditorGUILayout.LabelField("Operator", GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField("Min", GUILayout.MaxWidth(100));
					EditorGUILayout.LabelField("Max", GUILayout.MaxWidth(100));
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

	[CustomPropertyDrawer(typeof(ImageryRasterOptions))]
	public class ImageryRasterOptionsDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = lineHeight;
			showPosition = EditorGUI.Foldout(position, showPosition, label.text);
			//EditorGUI.indentLevel++;
			if (showPosition)
			{
				foreach (var item in property)
				{
					var subproperty = item as SerializedProperty;
					position.height = lineHeight;
					position.y += lineHeight;
					EditorGUI.PropertyField(position, subproperty, true);
				}
			}
			//EditorGUI.indentLevel--;
			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int rows = (showPosition) ? 4 : 1;
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
			EditorGUI.PropertyField(position, property.FindPropertyRelative("layerSource"), true);
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

			EditorGUI.PropertyField(position, property.FindPropertyRelative("Id"), new GUIContent { text = "Source Id" });

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			return lineHeight;
		}
	}

	[CustomPropertyDrawer(typeof(TypeVisualizerTuple))]
	public class TypeVisualizerBaseDrawer : PropertyDrawer
	{
		static float lineHeight = EditorGUIUtility.singleLineHeight;
		bool showPosition = true;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position.height = lineHeight;

			EditorGUI.PropertyField(position, property.FindPropertyRelative("Stack"));

			EditorGUI.EndProperty();
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Reserve space for the total visible properties.
			int rows = 2;
			//Debug.Log("Height - " + rows * lineHeight);
			return (float)rows * lineHeight;
		}
	}
}