using UnityEditor;
using UnityEngine;

public class FindMissingReferencesInScene : MonoBehaviour {
	[MenuItem("Mapbox/DevTools/Find Missing references in scene")]
	public static void FindMissingReferences() {
		GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();

		foreach (var go in objects) {
			var components = go.GetComponents<Component>();

			foreach (var c in components) {
				if (c == null) {
					Debug.LogError("Missing script found on: " + FullObjectPath(go));
				} else {
					SerializedObject so = new SerializedObject(c);
					var sp = so.GetIterator();

					while (sp.NextVisible(true)) {
						if (sp.propertyType != SerializedPropertyType.ObjectReference) {
							continue;
						}

						if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0) {
							ShowError(FullObjectPath(go), sp.name);
						}
					}
				}
			}
		}
		Debug.LogError("find refs done");
	}

	private static void ShowError(string objectName, string propertyName) {
		Debug.LogError("Missing reference found in: " + objectName + ", Property : " + propertyName);
	}

	private static string FullObjectPath(GameObject go) {
		return go.transform.parent == null ? go.name : FullObjectPath(go.transform.parent.gameObject) + "/" + go.name;
	}
}