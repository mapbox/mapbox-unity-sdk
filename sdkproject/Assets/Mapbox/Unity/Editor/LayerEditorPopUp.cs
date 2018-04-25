using UnityEngine;
using UnityEditor;

public class LayerEditorPopUp : PopupWindowContent
{
	bool toggle1 = true;
	bool toggle2 = true;
	bool toggle3 = true;

	public override Vector2 GetWindowSize()
	{
		return new Vector2(200, 150);
	}

	public override void OnGUI(Rect rect)
	{
		EditorTileJSONData data = EditorTileJSONData.Instance;
		if(!data.tileJSONLoaded)
		{
			EditorGUILayout.LabelField("Loading . . .");
		}
		else
		{
			foreach(var item in data.LayerSourcesDictionary)
			{
				if(item.Value.Count>1) // more than one source has the layer
				{
					EditorGUILayout.LabelField("Shared sources", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					EditorGUILayout.SelectableLabel(item.Key);
					EditorGUI.indentLevel--;
				}
				else
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.SelectableLabel(item.Key);
					EditorGUI.indentLevel--;
				}
			}
		}
	}

	public override void OnOpen()
	{
		Debug.Log("Popup opened: " + this);
	}

	public override void OnClose()
	{
		Debug.Log("Popup closed: " + this);
	}
}