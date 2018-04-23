namespace Mapbox.Utils
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Ar;
	using Mapbox.Unity.Map;

	public class VisualizeNodeBase : MonoBehaviour
	{
		[SerializeField]
		NodeSyncBase _nodeBase;

		[SerializeField]
		Color _color;

		[SerializeField]
		float _lineWidth;

		[SerializeField]
		Material _nodeMaterial;

		LineRenderer _lineRend;

		[SerializeField]
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
			_nodeBase.NodeAdded += PlotRoute;

		}

		// TODO; Maybe add here something that is like. better. like... hmmm..
		// If you check new nodes every second... That way you don't have to have that nodeAdded callback event thingy...
		// Less depedence... Might be better. Way better. I dunno might laggg...

		private void PlotRoute()
		{
			// TODO: pooling here for new Spheres... This won't work for MapMatching Nodes.

			//var nodePos = _map.GeoToWorldPosition(_nodeBase.ReturnLatestNode().LatLon);
			//Instantiate(GameObject.CreatePrimitive(PrimitiveType.Sphere), nodePos, Quaternion.identity, _map.gameObject.transform);

			var nodes = _nodeBase.ReturnNodes();
			_lineRend.positionCount = nodes.Length;

			for (int i = 0; i < _nodeBase.ReturnNodes().Length; i++)
			{
				_lineRend.SetPosition(i, _map.GeoToWorldPosition(nodes[i].LatLon));
			}
		}

		private void OnDisable()
		{
			_nodeBase.NodeAdded -= PlotRoute;
		}
	}
}
