using Mapbox.Unity.Map;

namespace Mapbox.Examples
{
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine.UI;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using System.Collections.Generic;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Object Inspector Modifier")]
	public class ObjectInspectorModifier : GameObjectModifier
	{
		private Dictionary<GameObject, FeatureSelectionDetector> _detectors;
		private Canvas _canvas;
		private FeatureUiMarker _marker;
		private FeatureSelectionDetector _tempDetector;
		private Transform _root;

		public override void Initialize()
		{
			if (_detectors == null)
			{
				_detectors = new Dictionary<GameObject, FeatureSelectionDetector>();
			}

			if (_marker == null)
			{
				if(_root == null)
				{
					_root = FindObjectOfType<AbstractMap>().transform;
				}

				var go = new GameObject("InteractiveSelectionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
				go.transform.SetParent(_root);
				_canvas = go.GetComponent<Canvas>();
				_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

				var sel = Instantiate(Resources.Load<GameObject>("selector"));
				sel.transform.SetParent(_canvas.transform);
				_marker = sel.GetComponent<FeatureUiMarker>();
			}
		}

		public override void Run(VectorEntity ve, UnityTile tile)
		{
			if (_detectors.ContainsKey(ve.GameObject))
			{
				_detectors[ve.GameObject].Initialize(_marker, ve);
			}
			else
			{
				_tempDetector = ve.GameObject.AddComponent<FeatureSelectionDetector>();
				_detectors.Add(ve.GameObject, _tempDetector);
				_tempDetector.Initialize(_marker, ve);
			}
		}

		public override void Clear()
		{
			if (_canvas != null)
			{
				_canvas.gameObject.Destroy();
			}
		}
	}
}