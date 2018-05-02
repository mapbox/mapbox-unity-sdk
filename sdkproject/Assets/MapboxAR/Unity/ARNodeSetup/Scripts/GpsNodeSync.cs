namespace Mapbox.Unity.Ar
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Utils;
	using Mapbox.Unity.Location;
	using System;
	using Mapbox.Unity.Map;
	//using System.Threading.Tasks;

	///<summary>
	///  Generates GPSNodes for ARLocationManager.
	/// </summary>
	public class GpsNodeSync : NodeSyncBase
	{
		[SerializeField]
		bool _filterNodes;

		[Tooltip("Applies only if FilterNode is true")]
		[SerializeField]
		float _desiredAccuracy = 5;

		[SerializeField]
		float _minMagnitude;

		List<Node> _savedNodes;

		public override void InitializeNodeBase()
		{
			_savedNodes = new List<Node>();
			IsNodeBaseInitialized = true;
			Debug.Log("Initialized GPS nodes");

		}

		private bool IsNodeGoodToUse(Location location)
		{
			var _map = LocationProviderFactory.Instance.mapManager;
			// Check Node accuracy & distance.
			var latestNode = _map.GeoToWorldPosition(location.LatitudeLongitude);
			var previousNode = _map.GeoToWorldPosition(_savedNodes[_savedNodes.Count - 1].LatLon);
			var forMagnitude = latestNode - previousNode;

			if (location.Accuracy <= _desiredAccuracy && _minMagnitude <= forMagnitude.magnitude)
			{
				// Node is good to use, return true
				return true;
			}
			else
			{
				//Bad node, discard. 
				return false;
			}
		}

		public override void SaveNode()
		{
			var location = LocationProviderFactory.Instance.DefaultLocationProvider.CurrentLocation;
			bool isFirstNode = (_savedNodes.Count == 0);
			bool isGoodFilteredNode = false;
			bool saveNode = true;
			if (isFirstNode)
			{
				saveNode = true;
			}
			else
			{
				isGoodFilteredNode = (_filterNodes && IsNodeGoodToUse(location));
				saveNode = true && ((!_filterNodes) || isGoodFilteredNode);
			}

			if (saveNode)
			{
				Debug.Log("Saving GPS Node");
				var latestNode = new Node();
				latestNode.LatLon = location.LatitudeLongitude;
				latestNode.Accuracy = location.Accuracy;
				_savedNodes.Add(latestNode);
			}
		}

		public override Node ReturnLatestNode()
		{
			return _savedNodes[_savedNodes.Count - 1]; ;
		}

		public override Node[] ReturnNodes()
		{
			return _savedNodes.ToArray();
		}
	}
}

