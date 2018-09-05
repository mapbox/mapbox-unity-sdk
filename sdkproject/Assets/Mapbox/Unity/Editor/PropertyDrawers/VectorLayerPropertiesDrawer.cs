namespace Mapbox.Editor
{
	using System;
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.Map;

	[CustomPropertyDrawer(typeof(VectorLayerProperties))]
	public class VectorLayerPropertiesDrawer : PropertyDrawer
	{
		private string objectId = "";
		/// <summary>
		/// Gets or sets a value to show or hide Vector section <see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> if show vector; otherwise, <c>false</c>.</value>
		bool ShowLocationPrefabs
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorLayerProperties_showLocationPrefabs");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorLayerProperties_showLocationPrefabs", value);
			}
		}

		/// <summary>
		/// Gets or sets a value to show or hide Vector section <see cref="T:Mapbox.Editor.MapManagerEditor"/>.
		/// </summary>
		/// <value><c>true</c> if show vector; otherwise, <c>false</c>.</value>
		bool ShowFeatures
		{
			get
			{
				return EditorPrefs.GetBool(objectId + "VectorLayerProperties_showFeatures");
			}
			set
			{
				EditorPrefs.SetBool(objectId + "VectorLayerProperties_showFeatures", value);
			}
		}

		private GUIContent _requiredMapIdGui = new GUIContent
		{
			text = "Required Map Id",
			tooltip = "For location prefabs to spawn the \"streets-v7\" tileset needs to be a part of the Vector data source"
		};

		FeaturesSubLayerPropertiesDrawer _vectorSublayerDrawer = new FeaturesSubLayerPropertiesDrawer();
		PointsOfInterestSubLayerPropertiesDrawer _poiSublayerDrawer = new PointsOfInterestSubLayerPropertiesDrawer();

		void ShowSepartor()
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			EditorGUILayout.Space();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, null, property);
			objectId = property.serializedObject.targetObject.GetInstanceID().ToString();
			var layerSourceProperty = property.FindPropertyRelative("sourceOptions");
			var sourceTypeProperty = property.FindPropertyRelative("_sourceType");
			VectorSourceType sourceTypeValue = (VectorSourceType)sourceTypeProperty.enumValueIndex;
			string streets_v7 = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets).Id;
			var layerSourceId = layerSourceProperty.FindPropertyRelative("layerSource.Id");
			string layerString = layerSourceId.stringValue;

			//Draw POI Section
			if(sourceTypeValue == VectorSourceType.None)
			{
				return;
			}

			ShowLocationPrefabs = EditorGUILayout.Foldout(ShowLocationPrefabs, "POINTS OF INTEREST");
			if (ShowLocationPrefabs)
			{
				if (sourceTypeValue != VectorSourceType.None && layerString.Contains(streets_v7))
				{
					GUI.enabled = false;
					EditorGUILayout.TextField(_requiredMapIdGui, streets_v7);
					GUI.enabled = true;
					_poiSublayerDrawer.DrawUI(property);
				}
				else
				{
					EditorGUILayout.HelpBox("In order to place points of interest please add \"mapbox.mapbox-streets-v7\" to the data source.", MessageType.Error);
				}
			}

			ShowSepartor();

			//Draw Feature section. 
			ShowFeatures = EditorGUILayout.Foldout(ShowFeatures, "FEATURES");
			if (ShowFeatures)
			{
				_vectorSublayerDrawer.DrawUI(property);
			}

			EditorGUI.EndProperty();
		}
	}
}
