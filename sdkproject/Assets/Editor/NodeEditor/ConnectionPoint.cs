using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NodeEditorNamespace
{
	public enum ConnectionPointType { In, Out }

	public class ConnectionPoint
	{
		public Rect rect;
		public Rect labelRect;
		public ConnectionPointType type;
		public Node node;
		public GUIStyle style;

		private string _name;
		private float _deltaY;
		private GUIStyle _labelStyle = new GUIStyle()
		{
			fontSize = 10,
			normal = new GUIStyleState() { textColor = Color.white },
			alignment = TextAnchor.MiddleRight
		};

		public ConnectionPoint(Node node, string name, float deltay, ConnectionPointType type, GUIStyle style)
		{
			this._name = Regex.Replace(name, "(\\B[A-Z])", " $1");
			this.node = node;
			this.type = type;
			this.style = style;
			_deltaY = deltay;
			rect = new Rect(0, 0, 10f, 20f);
		}

		public void Draw()
		{
			rect.y = node.rect.y + _deltaY - rect.height * 0.5f;
			labelRect = new Rect(node.rect.xMin, node.rect.y + _deltaY - 15f, node.rect.width - 20, 25);

			switch (type)
			{
				case ConnectionPointType.In:
					rect.x = node.rect.x - rect.width + 8f;
					break;

				case ConnectionPointType.Out:
					rect.x = node.rect.x + node.rect.width - 8f;
					break;
			}

			if (!string.IsNullOrEmpty(_name))
			{
				GUI.Label(labelRect, _name, _labelStyle);
			}

			if (GUI.Button(rect, "", style))
			{

			}
		}
	}
}