namespace Mapbox.Utils
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Ar;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Location;

	public class VisualizeNodeBase : MonoBehaviour
	{
		[SerializeField]
		NodeSyncBase _nodeBase;

		[SerializeField]
		Color _color;

		[SerializeField]
		float _lineWidth;

		[SerializeField]
		float _lineHeight;

		[SerializeField]
		Material _nodeMaterial;

		LineRenderer _lineRend;

		AbstractMap _map;

		private void Start()
		{
			_lineRend = gameObject.AddComponent<LineRenderer>();
			_lineRend.startColor = _color;
			_lineRend.endColor = _color;
			_lineRend.startWidth = _lineWidth;
			_lineRend.endWidth = _lineWidth;
			_lineRend.material = _nodeMaterial;
			_lineRend.useWorldSpace = false;
			_map = LocationProviderFactory.Instance.mapManager;
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += PlotRoute;
		}

		private void PlotRoute(Location location)
		{
			if (_nodeBase.IsNodeBaseInitialized)
			{
				var nodes = _nodeBase.ReturnNodes();
				var length = nodes.Length;
				_lineRend.positionCount = length;

				for (int i = 0; i < _nodeBase.ReturnNodes().Length; i++)
				{
					var position = _map.GeoToWorldPosition(nodes[i].LatLon, false);
					if (_lineHeight > 0)
					{
						position.y = _lineHeight;
					}

					_lineRend.SetPosition(i, position);
				}
			}
		}
	}
}
