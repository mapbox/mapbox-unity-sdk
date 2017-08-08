using System;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Modifiers;

public class Node
{
	[NonSerialized]
	private bool _isRoot = false;
	[NonSerialized]
	private Vector2 _panDelta;
	[NonSerialized]
	private float _nodeHeight = 50;
	[NonSerialized]
	private Vector2 _topLeft = new Vector2(50, 50);
	[NonSerialized]
	private Vector2 _padding = new Vector2(50, 100);
	private Vector2 drag;
	[NonSerialized]
	private float _propTopTest = 0f;

	public List<Connection> Connections;
	public List<Node> Children;

	public ScriptableObject ScriptableObject;
	public Rect rect;
	public string title;
	public string subtitle;
	public bool isDragged;
	public bool isSelected;

	public ConnectionPoint inPoint;
	public ConnectionPoint outPoint;

	public GUIStyle nodeStyle;
	public GUIStyle defaultNodeStyle;
	public GUIStyle selectedNodeStyle;
	public GUIStyle inPointStyle;
	public GUIStyle outPointStyle;
	
	public Action<Node> OnRemoveNode;

	private GUIStyle _titleStyle = new GUIStyle() {
		fontSize = 14,
		fontStyle = FontStyle.Bold,
		normal = new GUIStyleState() { textColor = Color.white }
	};

	private GUIStyle _subtitleStyle = new GUIStyle()
	{
		fontSize = 10,
		fontStyle = FontStyle.Italic,
		normal = new GUIStyleState() { textColor = Color.white }
	};

	//Vector2 position, float width, float height
	public Node(GUIStyle ns, GUIStyle ss, GUIStyle ips, GUIStyle ops, ScriptableObject so = null)
	{
		_propTopTest = 0f;
		Children = new List<Node>();
		Connections = new List<Connection>();
		ScriptableObject = so;
		//var w = width;
		if (ScriptableObject != null)
		{
			title = Regex.Replace(ScriptableObject.name, "(\\B[A-Z])", " $1");
			subtitle = Regex.Replace(ScriptableObject.GetType().Name, "(\\B[A-Z])", " $1");
			//w = title.Length * 10;
		}
		//rect = new Rect(position.x, position.y, w, height);
		nodeStyle = ns;
		inPoint = new ConnectionPoint(this, ConnectionPointType.In, ips);
		outPoint = new ConnectionPoint(this, ConnectionPointType.Out, ops);
		defaultNodeStyle = ns;
		selectedNodeStyle = ss;
		this.inPointStyle = ips;
		this.outPointStyle = ops;
	}
		
	public float Draw(Vector2 position, float width, float height, bool drawModifiers)
	{
		if (!drawModifiers && ScriptableObject is ModifierBase)
			return 0f;

		width = title.Length * 10;
		rect = new Rect(position.x + drag.x, position.y + drag.y, width, height);
		_propTopTest = 0;
		foreach (var c in Children)
		{
			var h = c.Draw(new Vector2(rect.xMax + _padding.x, rect.yMin + _propTopTest), 100, _nodeHeight, drawModifiers);
			_propTopTest += h;
			rect.height += h;
		}

		if(!_isRoot)
			inPoint.Draw(true);
		if (Children.Count > 0)
		{
			outPoint.Draw(false);
			rect.height -= Math.Min(_propTopTest, _nodeHeight);
		}
		//GUI.Box(rect, title, style);
		GUILayout.BeginArea(rect, nodeStyle);
		GUILayout.Label(title, _titleStyle);
		GUILayout.Label(subtitle, _subtitleStyle);
		GUILayout.EndArea();

		DrawConnections();

		return Math.Max(height, rect.height);
	}

	public bool ProcessEvents(Event e)
	{
		switch (e.type)
		{
			case EventType.MouseDown:
				if (e.button == 0)
				{
					if (rect.Contains(e.mousePosition))
					{
						isDragged = true;
						GUI.changed = true;
						isSelected = true;
						nodeStyle = selectedNodeStyle;
					}
					else
					{
						GUI.changed = true;
						isSelected = false;
						nodeStyle = defaultNodeStyle;
					}
				}

				if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
				{
					ProcessContextMenu();
					e.Use();
				}
				break;

			case EventType.MouseUp:
				isDragged = false;
				break;

			//case EventType.MouseDrag:
			//	if (e.button == 0 && isDragged)
			//	{
			//		rect.position += e.delta;
			//		e.Use();
			//		return true;
			//	}
			//	break;
		}

		return false;
	}

	private void ProcessContextMenu()
	{
		GenericMenu genericMenu = new GenericMenu();
		genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
		genericMenu.ShowAsContext();
	}

	private void OnClickRemoveNode()
	{
		if (OnRemoveNode != null)
		{
			OnRemoveNode(this);
		}
	}

	private void DrawConnections()
	{
		if (Connections != null)
		{
			for (int i = 0; i < Connections.Count; i++)
			{
				Connections[i].Draw();
			}
		}
	}

	public void Dive(object obj, Node parent, int depth = 0)
	{
		_isRoot = depth == 0;

		foreach (FieldInfo fi in obj.GetType().GetFields())
		{
			//field SO
			if (typeof(ScriptableObject).IsAssignableFrom(fi.FieldType))
			{
				var val = fi.GetValue(obj) as ScriptableObject;
				if (val != null)
				{
					//if (!heightFixed)
					//{
					//	heightFixed = true;
					//	parent.rect.height -= _nodeHeight;
					//}

					//new Vector2(parent.rect.xMax + _padding.x, parent.rect.yMin + propTop), 100, _nodeHeight, 
					var newNode = new Node(nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, val);
					Children.Add(newNode);
					newNode.Connections.Add(new Connection(newNode.inPoint, parent.outPoint));

					newNode.Dive(val, newNode, depth + 1);
				}
			}

			//field list<SO>
			Type type = fi.FieldType;
			if (type.IsGenericType && type.GetGenericTypeDefinition()
					== typeof(List<>))
			{
				if (typeof(ScriptableObject).IsAssignableFrom(type.GetGenericArguments()[0]))
				{
					var val = fi.GetValue(obj);
					if (val is IEnumerable)
					{
						foreach (ScriptableObject listitem in val as IEnumerable)
						{
							//new Vector2(parent.rect.xMax + _padding.x, parent.rect.yMin + propTop), 100, _nodeHeight, 
							var newNode = new Node(nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, listitem);
							Children.Add(newNode);
							newNode.Connections.Add(new Connection(newNode.inPoint, parent.outPoint));
							newNode.Dive(listitem, newNode, depth + 1);
						}

					}
				}
			}
		}

		foreach (PropertyInfo pi in obj.GetType().GetProperties())
		{
			//property SO
			if (typeof(ScriptableObject).IsAssignableFrom(pi.PropertyType))
			{
				var val = pi.GetValue(obj, null) as ScriptableObject;
				if (val != null)
				{
					//new Vector2(parent.rect.xMax + _padding.x, parent.rect.yMin + propTop + _padding.y), 100, _nodeHeight, 
					var newNode = new Node(nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, val);
					Children.Add(newNode);
					newNode.Connections.Add(new Connection(newNode.inPoint, parent.outPoint));
					newNode.Dive(val, newNode, depth + 1);
				}
			}

			//property list<SO>
			Type type = pi.PropertyType;
			if (type.IsGenericType && type.GetGenericTypeDefinition()
					== typeof(List<>))
			{
				if (typeof(ScriptableObject).IsAssignableFrom(type.GetGenericArguments()[0]))
				{
					var val = pi.GetValue(obj, null);
					if (val is IEnumerable)
					{
						foreach (ScriptableObject listitem in val as IEnumerable)
						{
							//new Vector2(parent.rect.xMax + _padding.x, parent.rect.yMin + propTop), 100, _nodeHeight, 
							var newNode = new Node(nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, listitem);
							Children.Add(newNode);
							newNode.Connections.Add(new Connection(newNode.inPoint, parent.outPoint));
							newNode.Dive(listitem, newNode, depth + 1);
						}

					}
				}
			}
		}
	}

	public void ProcessNodeEvents(Event e)
	{
		if (Children != null)
		{
			for (int i = Children.Count - 1; i >= 0; i--)
			{
				bool guiChanged = Children[i].ProcessEvents(e);

				if (guiChanged)
				{
					GUI.changed = true;
				}

				if (Children[i].isSelected)
					Selection.objects = new UnityEngine.Object[1] { Children[i].ScriptableObject };
			}
		}
	}
}