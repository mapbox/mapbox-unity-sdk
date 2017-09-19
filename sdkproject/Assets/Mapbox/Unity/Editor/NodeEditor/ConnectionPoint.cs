using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Mapbox.Editor.NodeEditor
{
	public enum ConnectionPointType { In, Out }

	public class ConnectionPoint
	{
		public Vector2 left;
		public Rect rect;
		public Rect labelRect;
		public Rect inLabelRect;
		public ConnectionPointType type;
		public Node node;
		public GUIStyle style;

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

		public ConnectionPoint(Node node, string inname, string name, float deltay, ConnectionPointType type, GUIStyle style)
		{
			this._outLabel = Regex.Replace(name, "(\\B[A-Z])", " $1");
			inLabel = inname;
			this.node = node;
			this.type = type;
			this.style = style;
			_deltaY = deltay;
			rect = new Rect(0, 0, 10f + (string.IsNullOrEmpty(inLabel) ? 0 : 100), 20f);
			left = new Vector2(rect.x, rect.y + (rect.height / 2));
			labelRect = new Rect(node.rect.xMin, node.rect.y + _deltaY - 15f, node.rect.width - 20, 25);
			inLabelRect = new Rect(rect.x + 4, rect.y - 1, rect.width, rect.height);
		}

		public void Draw()
		{
			rect.y = node.rect.y + _deltaY - rect.height * 0.5f;
			labelRect.x = node.rect.xMin;
			labelRect.y = node.rect.y + _deltaY - 15f;
			labelRect.width = node.rect.width - 20;
			inLabelRect.x = rect.x + 4;
			inLabelRect.y = rect.y - 1;
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
			if (GUI.Button(rect, "", style))
			{

			}
			
			GUI.Label(inLabelRect, inLabel, _inLabelStyle);
		}
	}
}