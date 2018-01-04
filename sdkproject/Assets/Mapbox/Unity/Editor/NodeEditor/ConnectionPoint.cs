using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Mapbox.Editor.NodeEditor
{
	public enum ConnectionPointType { In, Out }

	public class ConnectionPoint
	{
		public Vector2 left;
		public Vector2 right;
		public Rect rect;
		public Rect labelRect;
		public Rect inLabelRect;
		public Rect toggleRect;
		public ConnectionPointType type;
		public Node node;
		public GUIStyle style;
		private SerializedProperty _activeProp;
		public bool isActive;

		private string _outLabel;
		private string _inLabel;
		public string inLabel
		{
			get { return _inLabel; }
			set
			{
				if (_inLabel != value)
					labelGui = new GUIContent(value.ToString());
				_inLabel = value;
			}
		}
		private GUIContent labelGui;

		private float _deltaY;
		private GUIStyle _labelStyle = new GUIStyle()
		{
			fontSize = 10,
			normal = new GUIStyleState() { textColor = Color.white },
			alignment = TextAnchor.MiddleRight
		};
		private GUIStyle _inLabelStyle = new GUIStyle()
		{
			fontSize = 10,
			normal = new GUIStyleState() { textColor = Color.white },
			alignment = TextAnchor.MiddleRight
		};

		private static Texture2D activeOutImage;
		private static Texture2D inactiveOutImage;

		public ConnectionPoint(Node node, string inname, string name, float deltay, ConnectionPointType type, GUIStyle style, SerializedProperty activeProp = null)
		{
			isActive = true;
			if (activeOutImage == null)
			{
				activeOutImage = EditorGUIUtility.Load("builtin skins/darkskin/images/node3.png") as Texture2D;
				inactiveOutImage = EditorGUIUtility.Load("builtin skins/darkskin/images/node6.png") as Texture2D;
			}

			if (!string.IsNullOrEmpty(name))
			{
				this._outLabel = Regex.Replace(name, "(\\B[A-Z])", " $1");
			}
			else
			{
				this._outLabel = "";
			}
			inLabel = inname;
			this.node = node;
			this.type = type;
			this.style = style;
			_deltaY = deltay;
			rect = new Rect(0, 0, 10f + (string.IsNullOrEmpty(inLabel) ? 0 : 100), 20f);
			left = new Vector2(rect.x, rect.y + (rect.height / 2));
			
			labelRect = new Rect(node.rect.xMin, node.rect.y + _deltaY - 15f, node.rect.width - 20, 25);
			inLabelRect = new Rect(rect.x + 4, rect.y - 1, rect.width, rect.height);

			_activeProp = activeProp;
		}

		public void Draw()
		{
			if (_activeProp != null)
				isActive = _activeProp.boolValue;

			rect.y = node.rect.y + _deltaY - rect.height * 0.5f;
			labelRect.x = node.rect.xMin + (_activeProp != null ? -20 : 0);
			labelRect.y = node.rect.y + _deltaY - 15f;
			labelRect.width = node.rect.width - 20;
			inLabelRect.x = rect.x + 4;
			inLabelRect.y = rect.y - 1;

			toggleRect.x = node.rect.xMin - 30 + node.rect.width;
			toggleRect.width = 20;
			toggleRect.y = labelRect.y + 5;
			toggleRect.height = 20;

			switch (type)
			{
				case ConnectionPointType.In:
					rect.x = node.rect.x - rect.width + 8f;
					break;

				case ConnectionPointType.Out:
					rect.x = node.rect.x + node.rect.width - 8f;
					break;
			}
			
			if (!string.IsNullOrEmpty(_outLabel))
			{
				GUI.Label(labelRect, _outLabel, _labelStyle);
			}

			if (!string.IsNullOrEmpty(inLabel))
			{
				var v = _inLabelStyle.CalcSize(labelGui);
				inLabelRect.x = node.rect.x - v.x - 13;
				inLabelRect.width = v.x + 13;
				rect.x = node.rect.x - v.x - 5;
				rect.width = v.x + 15;
			}
			left.x = rect.x;
			left.y = rect.y + (rect.height / 2);

			if (_activeProp != null)
			{
				rect.x -= 30;
				rect.width = 45;
				rect.y -= 1;
				rect.height = 21;

				if (_activeProp.boolValue)
				{
					GUI.DrawTexture(rect, activeOutImage);
				}
				else
				{
					GUI.DrawTexture(rect, inactiveOutImage);
				}
			}
			else
			{
				if (GUI.Button(rect, "", style))
				{

				}
			}

			if (_activeProp != null)
			{
				_activeProp.boolValue = EditorGUI.Toggle(toggleRect, _activeProp.boolValue);
				_activeProp.serializedObject.ApplyModifiedProperties();
			}

			GUI.Label(inLabelRect, inLabel, _inLabelStyle);
		}
	}
}