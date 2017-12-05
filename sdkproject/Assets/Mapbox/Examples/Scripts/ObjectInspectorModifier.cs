namespace Mapbox.Examples
{
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine.UI;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Object Inspector Modifier")]
	public class ObjectInspectorModifier : GameObjectModifier
	{
		private FeatureUiMarker _marker;

		public override void Run(VectorEntity ve, UnityTile tile)
		{
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

			var det = ve.GameObject.AddComponent<FeatureSelectionDetector>();
			det.Initialize(_marker, ve);
		}
	}
}