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

		void Start()
		{

			// this start method as well could be eliminated all together....
			// And added to the run method.. Although... yeah nevermind...

			Action<Location> SaveFirstNode = null;
			SaveFirstNode = (Location location) =>
			{
				SaveNodes(location);
				LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated -= SaveFirstNode;
				LocationProviderFactory.Instance.DeviceLocationProvider.OnLocationUpdated += FilterNodes;
			};
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += SaveFirstNode;
			_savedNodes = new List<Node>();
		}

		//TODO : So basically the GPS can be 15 off and then 4 off.. And it might send the location 
		//Pretty often. So basically if the player has not moved over certain threshold in ARCoordsSpace replace the latest GPS Node location with the new location...
		//This is for pedestrians.

		//Also need to check it out if the ARNode accuracy is lower than GPS accuracy to erase all the ARnodes and
		// Start Placing AR nodes again...... Also if AR tracking goes really low then replace placement with the latest GPS node again... When tracking state jumps up...
		//All of this should happen in the CentralizedARLocator.

		private void FilterNodes(Location location)
		{
			Debug.Log("GPS nodes runs");

			if (_filterNodes)
			{
				// Check Node accuracy & distance.
				var latestNode = _map.GeoToWorldPosition(location.LatitudeLongitude);
				var previousNode = _map.GeoToWorldPosition(_savedNodes[_savedNodes.Count - 1].LatLon);
				var forMagnitude = latestNode - previousNode;

				if (location.Accuracy <= _desiredAccuracy && _minMagnitude >= forMagnitude.magnitude)
				{
					// Save nodes.. that are more accurate than 
					SaveNodes(location);
				}
			}
			else
			{
				//Saves every node.
				SaveNodes(location);
			}
		}

		private void SaveNodes(Location location)
		{
			var latestNode = new Node();
			latestNode.LatLon = location.LatitudeLongitude;
			latestNode.Accuracy = location.Accuracy;
			_savedNodes.Add(latestNode);

			if (NodeAdded != null)
			{
				NodeAdded();
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

