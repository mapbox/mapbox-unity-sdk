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
		AbstractMap _map;

		[SerializeField]
		float _secondsBetweenDropCheck, _minMagnitudeBetween;

		IEnumerator _saveARnodes;
		List<Node> _savedNodes;
		WaitForSeconds _waitFor;
		int _latestBestGPSAccuracy;


		public override void InitializeNodeBase()
		{
			_waitFor = new WaitForSeconds(_secondsBetweenDropCheck);
			_savedNodes = new List<Node>();
			//{
			//	new Node
			//	{
			//		LatLon = _map.WorldToGeoPosition(_targetTransform.position),
			//		Accuracy = _latestBestGPSAccuracy
			//	}
			//};
			CentralizedARLocator.OnNewHighestAccuracyGPS += SavedGPSAccuracy;
			Debug.Log("Initialized ARNodes");
			//SaveArNodes(_targetTransform);
		}

		void SavedGPSAccuracy(Location location)
		{
			_latestBestGPSAccuracy = location.Accuracy;
		}

		//async void SaveArNodes(Transform dropTransform)
		//{
		//	while (true)
		//	{
		//		ConvertToNodes(dropTransform);
		//		await Task.Delay(TimeSpan.FromSeconds(1));
		//	}
		//}

		public override void SaveNode()
		{
			bool saveNode = false;

			if (LocationProviderFactory.Instance.IsMapInitialized == true)
			{
				if (_savedNodes.Count > 1)
				{
					var previousNodePos = _map.GeoToWorldPosition(_savedNodes[_savedNodes.Count - 1].LatLon, false);
					var currentMagnitude = _targetTransform.position - previousNodePos;
					Debug.Log("ARNode Magnitude: " + currentMagnitude);
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
					var node = new Node();
					node.LatLon = _map.WorldToGeoPosition(_targetTransform.position);
					node.Accuracy = _latestBestGPSAccuracy;
					_savedNodes.Add(node);

					if (NodeAdded != null)
					{
						NodeAdded();
					}
				}
			}

		}
		//void ConvertToNodes(Transform nodeDrop)
		//{
		//	 Despise if else jungles...
		//	if (_savedNodes.Count >= 1)
		//	{
		//		var previousNodePos = _map.GeoToWorldPosition(_savedNodes[_savedNodes.Count - 1].LatLon, false);
		//		var currentMagnitude = nodeDrop.position - previousNodePos;
		//		Debug.Log("ARNode Magnitude: " + currentMagnitude);
		//		if (currentMagnitude.magnitude >= _minMagnitudeBetween)
		//		{
		//			var node = new Node();
		//			node.LatLon = _map.WorldToGeoPosition(nodeDrop.position);
		//			node.Accuracy = _latestBestGPSAccuracy;
		//			_savedNodes.Add(node);

		//			if (NodeAdded != null)
		//			{
		//				NodeAdded();
		//			}
		//		}
		//	}
		//	else
		//	{
		//		var node = new Node();
		//		node.LatLon = _map.WorldToGeoPosition(nodeDrop.position);
		//		node.Accuracy = _latestBestGPSAccuracy;
		//		_savedNodes.Add(node);

		//		if (NodeAdded != null)
		//		{
		//			NodeAdded();
		//		}
		//	}

		//}

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

