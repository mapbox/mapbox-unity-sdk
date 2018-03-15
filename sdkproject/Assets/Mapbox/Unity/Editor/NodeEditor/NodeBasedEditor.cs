using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using System.Reflection;
using System;
using System.Linq;
using System.Collections;

namespace Mapbox.Editor.NodeEditor
{
	public class NodeBasedEditor : EditorWindow
	{
		[NonSerialized]
		private Vector2 _panDelta;
		[NonSerialized]
		private float _nodeHeight = 50;
		[NonSerialized]
		private Vector2 _topLeft = new Vector2(50, 50);
		//[NonSerialized]
		//private Vector2 _padding = new Vector2(50, 100);

		//private List<Node> nodes;
		//private List<Connection> connections;
		private int _activeMap = 0;
		private List<Node> _maps;
		//private Node _rootNode;

		public static GUIStyle nodeStyle;
		public static GUIStyle leafNodeStyle;
		public static GUIStyle optionStyle;
		public static GUIStyle selectedNodeStyle;
		public static GUIStyle inPointStyle;
		public static GUIStyle outPointStyle;
		private static Texture2D _magnifierTexture;
		public static Texture2D magnifierTexture
		{
			get
			{
				if (_magnifierTexture == null)
				{
					_magnifierTexture = EditorGUIUtility.FindTexture("d_ViewToolZoom");
				}
				return _magnifierTexture;
			}
		}
		private GUIStyle _optionLabel;

		//private ConnectionPoint selectedInPoint;
		//private ConnectionPoint selectedOutPoint;

		private Vector2 offset;
		private Vector2 drag;
		private float zoomScale = 1;
		private Vector2 zoomOrigin = new Vector2(0, 20);
		private Rect _canvasWindowRect { get { return new Rect(0, 20, position.width, position.height - 20); } }
		private Rect _optionsRect { get { return new Rect(position.width - 250, 20, 250, 60); } }
		private bool _showOptions = false;
		private Vector2 _clickedPosition;

		[MenuItem("Mapbox/Map Editor")]
		private static void OpenWindow()
		{
			NodeBasedEditor window = GetWindow<NodeBasedEditor>();
			window.titleContent = new GUIContent("Map Editor");
		}

		private void OnEnable()
		{
			GUIScaleUtility.CheckInit();
			//MagnifierTexture = EditorGUIUtility.FindTexture("d_ViewToolZoom");
			var textOffset = new RectOffset(12, 0, 10, 0);

			nodeStyle = new GUIStyle();
			nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
			nodeStyle.border = new RectOffset(12, 12, 12, 12);
			nodeStyle.richText = true;
			nodeStyle.padding = textOffset;

			selectedNodeStyle = new GUIStyle();
			selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
			selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
			selectedNodeStyle.richText = true;
			selectedNodeStyle.padding = textOffset;

			leafNodeStyle = new GUIStyle();
			leafNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node2.png") as Texture2D;
			leafNodeStyle.border = new RectOffset(12, 12, 12, 12);
			leafNodeStyle.richText = true;
			leafNodeStyle.padding = textOffset;

			inPointStyle = new GUIStyle();
			inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
			inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
			inPointStyle.border = new RectOffset(4, 4, 12, 12);

			outPointStyle = new GUIStyle();
			outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
			outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
			outPointStyle.border = new RectOffset(4, 4, 12, 12);

			Parse();
		}

		private void Parse(bool showModifiers = true)
		{
			if (_maps == null)
				_maps = new List<Node>();
			else
				_maps.Clear();

			var abstractMaps = FindObjectsOfType<AbstractMap>();

			//foreach (var abstractMap in abstractMaps)
			//{
			//	foreach (FieldInfo fi in abstractMap.GetType().GetFields().Where(x => x.IsDefined(typeof(NodeEditorElementAttribute), true)))
			//	{
			//		var val = fi.GetValue(abstractMap) as ScriptableObject;
			//		if (typeof(ScriptableObject).IsAssignableFrom(fi.FieldType) && val != null)
			//		{
			//			var map = abstractMap.MapVisualizer;
			//			var mapNode = new Node(map as ScriptableObject);
			//			mapNode.title = map.name;
			//			mapNode.subtitle = "Map Visualizer";
			//			_maps.Add(mapNode);
			//			mapNode.Dive(map, showModifiers);
			//		}
			//	}
			//}

			foreach (var abstractMap in abstractMaps)
			{
				if (abstractMap != null)
				{
					var map = abstractMap;
					var mapNode = new Node(map);
					//{
					//	title = "Map",
					//	subtitle = "Map Visualizer"
					//};
					_maps.Add(mapNode);
					mapNode.Dive(map, showModifiers);
				}
			}
		}

		private void OnGUI()
		{
			if (optionStyle == null)
			{
				optionStyle = (GUIStyle)"ObjectPickerPreviewBackground";
				optionStyle.padding = new RectOffset(10, 10, 10, 10);
			}

			DrawGrid(20, 0.2f, Color.gray);
			DrawGrid(100, 0.4f, Color.gray);
			OnGUIToolBar();

			var test = _canvasWindowRect;
			var sc = GUIScaleUtility.BeginScale(ref test, zoomOrigin, zoomScale, false);

			if (_activeMap < _maps.Count)
			{
				DrawNodes(sc);

				ProcessNodeEvents(Event.current);
				ProcessEvents(Event.current);

				if (GUI.changed) Repaint();
			}
			GUIScaleUtility.EndScale();

			if (_showOptions)
			{
				GUILayout.BeginArea(_optionsRect, optionStyle);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Hide Modifiers", (GUIStyle)"ButtonLeft", GUILayout.Width(115)))
				{
					Parse(false);
				}
				if (GUILayout.Button("Show Modifiers", (GUIStyle)"ButtonRight", GUILayout.Width(115)))
				{
					Parse(true);
				}
				GUILayout.EndHorizontal();
				if (GUILayout.Button("Reset Zoom", EditorStyles.miniButton, GUILayout.Width(230)))
				{
					zoomScale = 1;
				}
				GUILayout.EndArea();
			}
		}

		void OnGUIToolBar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);

			if (GUILayout.Button("Map Visualizers", EditorStyles.toolbarDropDown))
			{
				GenericMenu toolsMenu = new GenericMenu();
				var i = 0;
				foreach (var item in _maps)
				{
					toolsMenu.AddItem(new GUIContent(item.title), false, ChangeMap, i);
					i++;
				}
				// Offset menu from right of editor window
				toolsMenu.DropDown(new Rect(0, 0, 0, 16));
				EditorGUIUtility.ExitGUI();
			}

			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(100)))
			{
				Parse();
			}
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Options", EditorStyles.toolbarButton, GUILayout.Width(100)))
			{
				_showOptions = !_showOptions;
			}

			GUILayout.EndHorizontal();
		}

		private void ChangeMap(object i)
		{
			_activeMap = (int)i;
		}

		void OnFocus()
		{
			Parse();
		}

		private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
		{
			int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
			int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

			Handles.BeginGUI();
			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			offset += drag * 0.5f;
			Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

			for (int i = 0; i < widthDivs; i++)
			{
				Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
			}

			for (int j = 0; j < heightDivs; j++)
			{
				Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
			}

			Handles.color = Color.white;
			Handles.EndGUI();
		}

		private void DrawNodes(Vector2 sc)
		{
			_maps[_activeMap].Draw(_topLeft + _panDelta + sc, 100, _nodeHeight);
			GUI.changed = true;
		}

		private void ProcessEvents(Event e)
		{
			drag = Vector2.zero;
			switch (e.type)
			{
				case EventType.MouseDown:
					if (e.button == 0)
					{
						_clickedPosition = e.mousePosition;
						ClearConnectionSelection();
					}
					break;

				case EventType.MouseDrag:
					if (e.button == 0)
					{
						if (_canvasWindowRect.Contains(_clickedPosition))
						{
							_panDelta += e.delta;
						}
					}
					break;

				case EventType.ScrollWheel:
					{

						Vector2 delta = Event.current.delta;
						Vector2 zoomedMousePos = (e.mousePosition - _canvasWindowRect.min) / zoomScale + zoomOrigin;

						float oldZoomScale = zoomScale;

						float zoomDelta = -delta.y / 150.0f;
						zoomScale -= zoomDelta;
						zoomScale = Mathf.Clamp(zoomScale, 0.5f, 2.0f);
						zoomOrigin += (zoomedMousePos - zoomOrigin) - (oldZoomScale / zoomScale) * (zoomedMousePos - zoomOrigin);

						Event.current.Use();

					}
					break;

				case EventType.MouseUp:
					if (e.button == 0)
					{
						_clickedPosition = new Vector2(Mathf.Infinity, Mathf.Infinity);
					}
					break;
			}
		}

		private void ProcessNodeEvents(Event e)
		{
			_maps[_activeMap].ProcessNodeEvents(e);
		}

		private void ClearConnectionSelection()
		{
			//selectedInPoint = null;
			//selectedOutPoint = null;
		}
	}
}