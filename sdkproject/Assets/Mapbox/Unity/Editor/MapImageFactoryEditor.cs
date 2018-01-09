namespace Mapbox.Editor
{
	using UnityEngine;
	using UnityEditor;
	using Mapbox.Unity.MeshGeneration.Factories;

	[CustomEditor(typeof(MapImageFactory))]
	public class MapImageFactoryEditor : FactoryEditor
	{
		public SerializedProperty
			mapIdType_Prop,
			basicMaps_Prop,
			customMapId_Prop,
			useMipMap_Prop,
		useCompression_Prop,
			useRetina_Prop,
			mapId_Prop;
		private MonoScript script;

		private string[] _basicMapIds = new string[6] {
		"mapbox://styles/mapbox/streets-v10",
		"mapbox://styles/mapbox/outdoors-v10",
		"mapbox://styles/mapbox/dark-v9",
		"mapbox://styles/mapbox/light-v9",
		"mapbox.satellite",
		"mapbox://styles/mapbox/satellite-streets-v10"};

		private string[] _basicMapNames = new string[6] {
		"Streets",
		"Outdoors",
		"Dark",
		"Light",
		"Satellite",
		"Satellite Street"};

		private int _choiceIndex = 0;
		void OnEnable()
		{
			customMapId_Prop = serializedObject.FindProperty("_customStyle");
			mapIdType_Prop = serializedObject.FindProperty("_mapIdType");
			mapId_Prop = serializedObject.FindProperty("_mapId");
			useMipMap_Prop = serializedObject.FindProperty("_useMipMap");
			useCompression_Prop = serializedObject.FindProperty("_useCompression");
			useRetina_Prop = serializedObject.FindProperty("_useRetina");
			script = MonoScript.FromScriptableObject((MapImageFactory)target);
			for (int i = 0; i < _basicMapIds.Length; i++)
			{
				if (_basicMapIds[i] == mapId_Prop.stringValue)
				{
					_choiceIndex = i;
					break;
				}
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUI.enabled = false;
			script = EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false) as MonoScript;
			GUI.enabled = true;
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(mapIdType_Prop, new GUIContent("Map Type"));
			EditorGUILayout.Space();
			var st = (MapImageType)mapIdType_Prop.enumValueIndex;
			EditorGUI.indentLevel++;

			switch (st)
			{
				case MapImageType.BasicMapboxStyle:
					{
						_choiceIndex = EditorGUILayout.Popup("Style", _choiceIndex, _basicMapNames);
						mapId_Prop.stringValue = _basicMapIds[_choiceIndex];
						GUI.enabled = false;
						EditorGUILayout.PropertyField(mapId_Prop, new GUIContent("Map Id"));
						GUI.enabled = true;
						break;
					}
				case MapImageType.Custom:
					{
						EditorGUILayout.PropertyField(customMapId_Prop, new GUIContent("Style Id"));
						mapId_Prop.stringValue = customMapId_Prop.FindPropertyRelative("Id").stringValue;
						if (string.IsNullOrEmpty(mapId_Prop.stringValue))
						{
							EditorGUILayout.HelpBox("Invalid MapID. This will cause invalid tile requests!", MessageType.Error);
						}
						break;
					}
				case MapImageType.None:
					break;

			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Raster Tile Texture Settings");
			EditorGUI.indentLevel++;

			EditorGUILayout.PropertyField(useCompression_Prop, new GUIContent("Use Compression"));
			if (useCompression_Prop.boolValue)
			{
				EditorGUILayout.HelpBox("Texture will be compressed. This will reduce image quality and lead to longer initialization times but save memory.", MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox("Use compression to save memory.", MessageType.Warning);
			}

			EditorGUILayout.PropertyField(useMipMap_Prop, new GUIContent("Create Mip Maps"));
			if (useMipMap_Prop.boolValue)
			{
				EditorGUILayout.HelpBox("Mip maps will consume additional memory but reduce noise at increasing distances.", MessageType.Warning);
			}
			EditorGUILayout.PropertyField(useRetina_Prop, new GUIContent("Request Retina-resolution"));
			if (useRetina_Prop.boolValue)
			{
				EditorGUILayout.HelpBox("Retina will consume additional memory but can greatly improve visual quality.", MessageType.Warning);
			}
			EditorGUI.indentLevel--;

			serializedObject.ApplyModifiedProperties();
		}
	}
}