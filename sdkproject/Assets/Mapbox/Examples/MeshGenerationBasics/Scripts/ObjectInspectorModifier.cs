namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Components;
	using UnityEngine.UI;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Object Inspector Modifier")]
	public class ObjectInspectorModifier : GameObjectModifier
	{
		private FeatureUiMarker _marker;

		public override void Run(FeatureBehaviour fb)
		{
			if(_marker == null)
			{
				var canv = FindObjectOfType<Canvas>();
				if (canv == null)
				{
					var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
					canv = go.GetComponent<Canvas>();
					canv.renderMode = RenderMode.ScreenSpaceOverlay;
				}

				var sel = Instantiate(Resources.Load<GameObject>("selector"));
				sel.transform.SetParent(canv.transform);
				_marker = sel.GetComponent<FeatureUiMarker>();
			}

			var det = fb.gameObject.AddComponent<FeatureSelectionDetector>();
			det.Initialize(_marker, fb);
		}
	}
}