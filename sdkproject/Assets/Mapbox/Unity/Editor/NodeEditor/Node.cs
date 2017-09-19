using System;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using Mapbox.Unity.MeshGeneration.Modifiers;
using System.Linq;
using Mapbox.Unity.MeshGeneration.Interfaces;

namespace Mapbox.Editor.NodeEditor
{
	public class Node
	{
		private bool _expanded = true;

		private bool _isRoot = false;
		private Vector2 _panDelta;
		//private Vector2 _topLeft = new Vector2(50, 50);
		private Vector2 _padding = new Vector2(50, 100);
		private float _propTopTest = 0f;

		public List<Connection> Connections;
		public List<ConnectionPoint> ConnectionPoints;
		public List<Node> Children;

		public ScriptableObject ScriptableObject;
		public Rect spaceRect;
		public Rect rect;
		public Rect buttonRect;
		public string title;
		public string subtitle;
		public bool isDragged;
		public bool isSelected;

		public ConnectionPoint inPoint;

		public Action<Node> OnRemoveNode;

		private GUIStyle _titleStyle = new GUIStyle()
		{
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

		private float _headerHeight = 70;
		private float _propertyHeight = 25;
		private int _propCount = 0;
		private float _inbuff;

		//Vector2 position, float width, float height
		public Node(ScriptableObject so = null)
		{
			_propTopTest = 0f;
			_propCount = 0;
			Children = new List<Node>();
			Connections = new List<Connection>();
			ConnectionPoints = new List<ConnectionPoint>();
			ScriptableObject = so;

			if (ScriptableObject != null)
			{
				title = Regex.Replace(ScriptableObject.name, "(\\B[A-Z])", " $1");
				subtitle = Regex.Replace(ScriptableObject.GetType().Name, "(\\B[A-Z])", " $1");
			}

			var inlabel = "";
			if (ScriptableObject is LayerVisualizerBase)
				inlabel = (ScriptableObject as VectorLayerVisualizer).Key;
			inPoint = new ConnectionPoint(this, inlabel, "", 20, ConnectionPointType.In, NodeBasedEditor.inPointStyle);
		}

		public float Draw(Vector2 position, float width, float height)
		{
			if (!string.IsNullOrEmpty(title))
				width = title.Length * 10;
			var boxHeight = _headerHeight + _propCount * _propertyHeight;
			if (ScriptableObject is ModifierBase)
				boxHeight = 52;
			_inbuff = (string.IsNullOrEmpty(inPoint.inLabel) ? 0 : 100);
			spaceRect = new Rect(position.x + _inbuff, position.y, width, boxHeight);
			rect = new Rect(position.x + _inbuff, position.y, width, boxHeight);
			buttonRect = new Rect(rect.xMax - 25, rect.yMin + 10, 20, 20);

			_propTopTest = 0;
			if (_expanded)
			{
				foreach (var c in Children)
				{
					var h = c.Draw(new Vector2(spaceRect.xMax + _padding.x, spaceRect.yMin + _propTopTest), 100, 0);
					_propTopTest += h;
					spaceRect.height += h;
				}
			}

			var so = ScriptableObject as VectorLayerVisualizer;
			if (so != null)
			{
				inPoint.inLabel = so.Key;
				inPoint.Draw();
			}
			else
			{
				if (!_isRoot)
					inPoint.Draw();
			}
			spaceRect.height = Math.Max(height, Math.Max(spaceRect.height, boxHeight));
			if (Children.Count > 0)
			{
				spaceRect.height -= Math.Min(_propTopTest, boxHeight);
			}

			if (isSelected)
			{
				GUILayout.BeginArea(rect, NodeBasedEditor.selectedNodeStyle);
			}
			else
			{
				if (ScriptableObject is ModifierBase)
					GUILayout.BeginArea(rect, NodeBasedEditor.leafNodeStyle);
				else
					GUILayout.BeginArea(rect, NodeBasedEditor.nodeStyle);
			}
			GUILayout.Label(title, _titleStyle);
			GUILayout.Label(subtitle, _subtitleStyle);
			GUILayout.EndArea();

			foreach (var p in ConnectionPoints)
			{
				p.Draw();
			}

			DrawConnections();

			if (Children.Count > 0)
			{
				if (_expanded)
				{
					if (GUI.Button(buttonRect, "", (GUIStyle)"flow overlay foldout"))
					{
						_expanded = !_expanded;
					}
				}
				else
				{
					if (GUI.Button(buttonRect, "", (GUIStyle)"IN FoldoutStatic"))
					{
						_expanded = !_expanded;
					}
				}
			}


			return spaceRect.height;
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
							if (!isDragged)
								Selection.objects = new UnityEngine.Object[1] { ScriptableObject };
							isDragged = true;
							GUI.changed = true;
							isSelected = true;
							//nodeStyle = selectedNodeStyle;
						}
						else
						{
							GUI.changed = true;
							isSelected = false;
							//nodeStyle = defaultNodeStyle;
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
			//GenericMenu genericMenu = new GenericMenu();
			//genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
			//genericMenu.ShowAsContext();
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

		public void Dive(object obj, bool showModifiers = true, int depth = 0)
		{
			if (obj == null)
				return;

			_isRoot = depth == 0;
			if (ScriptableObject is ModifierStackBase)
				_expanded = showModifiers;

			foreach (FieldInfo fi in obj.GetType().GetFields().Where(prop => prop.IsDefined(typeof(NodeEditorElementAttribute), false)))
			{
				//field SO
				if (typeof(ScriptableObject).IsAssignableFrom(fi.FieldType))
				{
					var val = fi.GetValue(obj) as ScriptableObject;
					if (val != null)
					{
						var name = (fi.GetCustomAttributes(typeof(NodeEditorElementAttribute), true)[0] as NodeEditorElementAttribute).Name;
						var conp = new ConnectionPoint(this, "", name, _headerHeight + _propertyHeight * _propCount, ConnectionPointType.Out, NodeBasedEditor.outPointStyle);
						ConnectionPoints.Add(conp);
						var newNode = new Node(val);
						Children.Add(newNode);
						newNode.Connections.Add(new Connection(newNode.inPoint, conp));

						newNode.Dive(val, showModifiers, depth + 1);
						_propCount++;
					}
				}

				//field list<SO>
				Type type = fi.FieldType;
				if (type.IsGenericType && type.GetGenericTypeDefinition()
						== typeof(List<>))
				{
					if (typeof(ScriptableObject).IsAssignableFrom(type.GetGenericArguments()[0]))
					{
						var name = (fi.GetCustomAttributes(typeof(NodeEditorElementAttribute), true)[0] as NodeEditorElementAttribute).Name;
						var conp = new ConnectionPoint(this, "", name, _headerHeight + _propertyHeight * _propCount, ConnectionPointType.Out, NodeBasedEditor.outPointStyle);
						ConnectionPoints.Add(conp);
						var val = fi.GetValue(obj);
						if (val is IEnumerable)
						{
							foreach (ScriptableObject listitem in val as IEnumerable)
							{
								var newNode = new Node(listitem);
								Children.Add(newNode);
								newNode.Connections.Add(new Connection(newNode.inPoint, conp));
								newNode.Dive(listitem, showModifiers, depth + 1);
							}
							_propCount++;
						}
					}
					else 
					{
						var val = fi.GetValue(obj);
						if (val is List<TypeVisualizerTuple>)
						{
							foreach (TypeVisualizerTuple listitem in val as IEnumerable)
							{
								//var name = (fi.GetCustomAttributes(typeof(NodeEditorElementAttribute), true)[0] as NodeEditorElementAttribute).Name;
								var cc = new ConnectionPoint(this, "", listitem.Type, _headerHeight + _propertyHeight * _propCount, ConnectionPointType.Out, NodeBasedEditor.outPointStyle);
								ConnectionPoints.Add(cc);
								_propCount++;
								var newNode = new Node(listitem.Stack);
								Children.Add(newNode);
								newNode.Connections.Add(new Connection(newNode.inPoint, cc));
								newNode.Dive(listitem.Stack, showModifiers, depth + 1);
							}
						}
					}
				}
			}

			foreach (PropertyInfo pi in obj.GetType().GetProperties().Where(prop => prop.IsDefined(typeof(NodeEditorElementAttribute), false)))
			{
				//property SO
				if (typeof(ScriptableObject).IsAssignableFrom(pi.PropertyType))
				{
					var val = pi.GetValue(obj, null) as ScriptableObject;
					if (val != null)
					{

						var name = (pi.GetCustomAttributes(typeof(NodeEditorElementAttribute), true)[0] as NodeEditorElementAttribute).Name;
						var conp = new ConnectionPoint(this, "", name, _headerHeight + _propertyHeight * _propCount, ConnectionPointType.Out, NodeBasedEditor.outPointStyle);
						ConnectionPoints.Add(conp);
						var newNode = new Node(val);
						Children.Add(newNode);
						newNode.Connections.Add(new Connection(newNode.inPoint, conp));
						newNode.Dive(val, showModifiers, depth + 1);
						_propCount++;
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

						var name = (pi.GetCustomAttributes(typeof(NodeEditorElementAttribute), true)[0] as NodeEditorElementAttribute).Name;
						var conp = new ConnectionPoint(this, "", name, _headerHeight + _propertyHeight * _propCount, ConnectionPointType.Out, NodeBasedEditor.outPointStyle);
						ConnectionPoints.Add(conp);
						if (val is IEnumerable)
						{
							foreach (ScriptableObject listitem in val as IEnumerable)
							{
								var newNode = new Node(listitem);
								Children.Add(newNode);
								newNode.Connections.Add(new Connection(newNode.inPoint, conp));
								newNode.Dive(listitem, showModifiers, depth + 1);
							}
							_propCount++;
						}
					}
				}
			}
		}

		public void ProcessNodeEvents(Event e)
		{
			bool guiChanged = ProcessEvents(e);

			foreach (var item in Children)
			{
				item.ProcessNodeEvents(e);
			}

			if (guiChanged)
			{
				GUI.changed = true;
			}
		}
	}
}