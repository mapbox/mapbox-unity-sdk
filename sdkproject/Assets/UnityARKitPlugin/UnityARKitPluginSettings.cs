using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds settings that are used to configure the Unity ARKit Plugin.
/// </summary>
[CreateAssetMenu(fileName = "ARKitSettings", menuName = "UnityARKitPlugin/Settings", order = 1)]
public class UnityARKitPluginSettings : ScriptableObject {

	/// <summary>
	/// Toggles whether Facetracking for iPhone X (and later) is used. If enabled, provide a Privacy Policy for submission to AppStore.
	/// </summary>
	[Tooltip("Toggles whether Facetracking for iPhone X (and later) is used. If enabled, provide a Privacy Policy for submission to AppStore.")]
	public bool m_ARKitUsesFacetracking = false;

	/// <summary>
	/// Toggles whether ARKit is required for this app: will make app only downloadable by devices with ARKit support if enabled.
	/// </summary>
	[Tooltip("Toggles whether ARKit is required for this app: will make app only downloadable by devices with ARKit support if enabled.")]
	public bool AppRequiresARKit = false;

}
