using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using System.Reflection;
using System;
using System.Linq;
using System.Collections;
using NodeEditorFramework.Utilities;

public class NodeBasedEditor : EditorWindow
{
	[NonSerialized]
	private Vector2 _panDelta;
	[NonSerialized]
	private float _nodeHeight = 50;
	[NonSerialized]
	private Vector2 _topLeft = new Vector2(50, 50);
	[NonSerialized]
	private Vector2 _padding = new Vector2(50, 100);

	//private List<Node> nodes;
	//private List<Connection> connections;
	private Node _rootNode;

	private GUIStyle nodeStyle;
	private GUIStyle optionStyle;
	private GUIStyle selectedNodeStyle;
	private GUIStyle inPointStyle;
	private GUIStyle outPointStyle;

	private ConnectionPoint selectedInPoint;
	private ConnectionPoint selectedOutPoint;

	private Vector2 offset;
	private Vector2 drag;
	private float zoomScale = 1;
	private Vector2 zoomOrigin = new Vector2(0, 20);
	private Rect _canvasWindowRect { get { return new Rect(0, 20, position.width, position.height -20); } }
	private Rect _optionsRect { get { return new Rect(position.width - 200, 20, 200, 40); } }
	private bool _showOptions = false;
	private bool _showModifiers = true;

	[MenuItem("Window/Node Based Editor")]
	private static void OpenWindow()
	{
		NodeBasedEditor window = GetWindow<NodeBasedEditor>();
		window.titleContent = new GUIContent("Node Based Editor");
	}

	private void OnEnable()
	{
		GUIScaleUtility.CheckInit();
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

	private void Parse()
	{
		var map = FindObjectOfType<AbstractMap>().MapVisualizer;
		var mapNode = new Node(nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, map as ScriptableObject);
		mapNode.title = map.name;
		mapNode.subtitle = "Map Visualizer";
		_rootNode = mapNode;

		_rootNode.Dive(map, mapNode);
	}
	
	private void OnGUI()
	{
		if (optionStyle == null)
		{
			optionStyle = (GUIStyle)"ObjectPickerPreviewBackground";
		}

		DrawGrid(20, 0.2f, Color.gray);
		DrawGrid(100, 0.4f, Color.gray);
		OnGUIToolBar();

		var test = _canvasWindowRect;
		GUIScaleUtility.BeginScale(ref test, zoomOrigin, zoomScale, true, true);

		DrawNodes();

		ProcessNodeEvents(Event.current);
		ProcessEvents(Event.current);

		if (GUI.changed) Repaint();
		GUIScaleUtility.EndScale();

		if(_showOptions)
		{
			GUILayout.BeginArea(_optionsRect, optionStyle);
			_showModifiers = EditorGUILayout.Toggle("Show Modifiers", _showModifiers);
			GUILayout.EndArea();
		}
	}

	void OnGUIToolBar()
	{
		GUILayout.BeginHorizontal(EditorStyles.toolbar);

		if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(100)))
		{
			Parse();
		}
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Options", EditorStyles.miniButton, GUILayout.Width(100)))
		{
			_showOptions = !_showOptions;
		}

		GUILayout.EndHorizontal();
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

	private void DrawNodes()
	{
		_rootNode.Draw(_topLeft + _panDelta, 100, _nodeHeight, _showModifiers);
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
					ClearConnectionSelection();
				}
				break;

			case EventType.MouseDrag:
				if (e.button == 0)
				{
					_panDelta += e.delta;
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
		}
	}

	private void ProcessNodeEvents(Event e)
	{
		_rootNode.ProcessNodeEvents(e);
	}
	
	private void ClearConnectionSelection()
	{
		selectedInPoint = null;
		selectedOutPoint = null;
	}
}