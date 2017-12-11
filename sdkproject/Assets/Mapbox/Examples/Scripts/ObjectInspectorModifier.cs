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
		private FeatureUiMarker _marker;
		private FeatureSelectionDetector _tempDetector;

		public override void Initialize()
		{
			if (_detectors == null)
			{
				_detectors = new Dictionary<GameObject, FeatureSelectionDetector>();
			}

			if (_marker == null)
			{
				Canvas canvas;
				var go = new GameObject("InteractiveSelectionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
				canvas = go.GetComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;

				var sel = Instantiate(Resources.Load<GameObject>("selector"));
				sel.transform.SetParent(canvas.transform);
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
	}
}