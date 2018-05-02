namespace Mapbox.Unity.Ar
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Location;
	using Mapbox.Utils;
	using System.Linq;

	/// <summary>
	///  Generates and filters ArNodes for ARLocationManager.
	/// </summary>
	public class ArNodesSync : NodeSyncBase
	{

		[SerializeField]
		Transform _targetTransform;

		[SerializeField]
		float _minMagnitudeBetween;

		float _latestBestGPSAccuracy;
		List<Node> _savedNodes;
		AbstractMap _map;
		CircularBuffer<Node> _nodeBuffer;

		public override void InitializeNodeBase(AbstractMap map)
		{
			_savedNodes = new List<Node>();
			CentralizedARLocator.OnNewHighestAccuracyGPS += SavedGPSAccuracy;
			_map = map;
			IsNodeBaseInitialized = true;
			Debug.Log("Initialized ARNodes");
			_nodeBuffer = new CircularBuffer<Node>(10);
			_nodeBuffer.GetEnumerable().ToArray();
		}

		void SavedGPSAccuracy(Location location)
		{
			_latestBestGPSAccuracy = location.Accuracy;
		}

		public override void SaveNode()
		{
			bool saveNode = false;

			if (_nodeBuffer.Count > 1)
			{
				var previousNodePos = _map.GeoToWorldPosition(_savedNodes[_savedNodes.Count - 1].LatLon, false);
				var currentMagnitude = _targetTransform.position - previousNodePos;

				if (currentMagnitude.magnitude >= _minMagnitudeBetween)
				{
					saveNode = true;
				}
			}
			else
			{
				saveNode = true;
			}
			if (saveNode == true)
			{
				Debug.Log("Saving AR Node");
				var node = new Node();
				node.LatLon = _map.WorldToGeoPosition(_targetTransform.position);
				node.Accuracy = _latestBestGPSAccuracy;
				_savedNodes.Add(node);
			}

		}

		public override Node[] ReturnNodes()
		{
			return _savedNodes.ToArray();
		}

		public override Node ReturnLatestNode()
		{
			return _savedNodes[_savedNodes.Count - 1];
		}
	}
}
