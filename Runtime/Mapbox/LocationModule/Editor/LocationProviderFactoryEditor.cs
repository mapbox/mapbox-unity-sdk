using Mapbox.LocationModule;
using Mapbox.LocationModule;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocationProviderFactory))]
public class LocationProviderFactoryEditor : Editor
{
	private SerializedProperty _locationProviderTypeProp;
	private SerializedProperty _mapProp;
	private SerializedProperty _editorLatLngProp;
	private SerializedProperty _unitySettingsProp;
	private SerializedProperty _mapboxSettingsProp;

	private bool _editorInitialized;

	// Editor.OnEnable can run while the target is null - entering Play mode destroys and recreates
	// the inspected object - and touching serializedObject then throws
	// SerializedObjectNotCreatableException, which cannot be recovered from inside the callback.
	// Initialization is retried from OnInspectorGUI, which only runs once the target is real.
	private void TryInitializeEditor()
	{
	    if (_editorInitialized || target == null)
	    {
	        return;
	    }

		_locationProviderTypeProp = serializedObject.FindProperty("LocationProviderType");
		_editorLatLngProp = serializedObject.FindProperty("EditorLatitudeLongitude");
		_unitySettingsProp = serializedObject.FindProperty("_unityLocationProviderSettings");
		_mapboxSettingsProp = serializedObject.FindProperty("_mapboxLocationProviderSettings");

	    _editorInitialized = true;
	}

	private void OnEnable()
	{
	    TryInitializeEditor();
	}

	public override void OnInspectorGUI()
	{
	    TryInitializeEditor();
	    if (!_editorInitialized)
	    {
	        return;
	    }

		serializedObject.Update();

		EditorGUILayout.PropertyField(_locationProviderTypeProp);

		EditorGUILayout.Space();

		var providerType =
			(LocationProviderType)_locationProviderTypeProp.enumValueIndex;

		switch (providerType)
		{
			case LocationProviderType.StaticLocationProvider:
				DrawEditorProvider();
				break;

			case LocationProviderType.UnityLocationProvider:
				DrawUnityProvider();
				break;

			case LocationProviderType.MapboxLocationProvider:
				DrawMapboxProvider();
				break;

			// case LocationProviderType.CustomLocationProvider:
			// 	EditorGUILayout.HelpBox(
			// 		"Custom location provider not implemented yet.",
			// 		MessageType.Info
			// 	);
			// 	break;
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void DrawEditorProvider()
	{
		EditorGUILayout.HelpBox(
			"Uses a fixed editor-only location.",
			MessageType.None
		);

		EditorGUILayout.PropertyField(_editorLatLngProp);
	}

	private void DrawUnityProvider()
	{
		EditorGUILayout.HelpBox(
			"Uses Unity Input.location service.",
			MessageType.None
		);

		EditorGUILayout.PropertyField(_unitySettingsProp);
	}

	private void DrawMapboxProvider()
	{
		EditorGUILayout.HelpBox(
			"Mapbox provider is experimental.",
			MessageType.Warning
		);

		EditorGUILayout.PropertyField(_mapboxSettingsProp);
	}
}
