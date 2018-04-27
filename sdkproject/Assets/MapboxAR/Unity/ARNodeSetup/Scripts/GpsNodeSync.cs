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
		AbstractMap _map;

		[SerializeField]
		bool _filterNodes;

		[Tooltip("Applies only if FilterNode is true")]
		[SerializeField]
		int _desiredAccuracy = 5;

		[SerializeField]
		float _minMagnitude;

		List<Node> _savedNodes;


		//TODO : So basically the GPS can be 15 off and then 4 off.. And it might send the location 
		//Pretty often. So basically if the player has not moved over certain threshold in ARCoordsSpace replace the latest GPS Node location with the new location...
		//This is for pedestrians.

		//Also need to check it out if the ARNode accuracy is lower than GPS accuracy to erase all the ARnodes and
		// Start Placing AR nodes again...... Also if AR tracking goes really low then replace placement with the latest GPS node again... When tracking state jumps up...
		//All of this should happen in the CentralizedARLocator.
		//private void Start()
		//{
		//	InitializeNodeBase();
		//}
		public override void InitializeNodeBase()
		{
			_savedNodes = new List<Node>();

			// Hack - adding a new node.

			//SaveNodes(LocationProviderFactory.Instance.DefaultLocationProvider.CurrentLocation);
			//LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += SaveNode;
			Debug.Log("Initialized GPS nodes");

		}

		private bool IsNodeGoodToUse(Location location)
		{
			Debug.Log("GPS nodes runs");

			// Check Node accuracy & distance.
			var latestNode = _map.GeoToWorldPosition(location.LatitudeLongitude);
			var previousNode = _map.GeoToWorldPosition(_savedNodes[_savedNodes.Count - 1].LatLon);
			var forMagnitude = latestNode - previousNode;
			Debug.Log("Location on GPS node : " + location.LatitudeLongitude);
			Debug.Log("Magnitude is: " + forMagnitude.magnitude);

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
			Debug.Log("GPS SaveNode");
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

				if (NodeAdded != null)
				{
					NodeAdded();
				}
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

