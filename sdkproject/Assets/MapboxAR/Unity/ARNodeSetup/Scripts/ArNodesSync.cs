namespace Mapbox.Unity.Ar
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Location;
	//using System.Threading.Tasks;

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
		WaitForSeconds _waitFor;

		AbstractMap _map;

		public override void InitializeNodeBase()
		{
			_savedNodes = new List<Node>();
			CentralizedARLocator.OnNewHighestAccuracyGPS += SavedGPSAccuracy;
			_map = LocationProviderFactory.Instance.mapManager;
			IsNodeBaseInitialized = true;
			Debug.Log("Initialized ARNodes");
		}

		void SavedGPSAccuracy(Location location)
		{
			_latestBestGPSAccuracy = location.Accuracy;
		}

		public override void SaveNode()
		{
			bool saveNode = false;

			if (_savedNodes.Count > 1)
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
