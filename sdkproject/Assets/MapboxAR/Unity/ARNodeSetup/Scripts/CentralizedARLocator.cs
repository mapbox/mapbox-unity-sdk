namespace Mapbox.Unity.Ar
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Location;
	using System;
	using Mapbox.MapMatching;
	using Mapbox.Utils;
	//using System.Threading.Tasks;
	using UnityARInterface;
	using Mapbox.Unity.Map;

	public class CentralizedARLocator : MonoBehaviour, ISynchronizationContext
	{

		// TODO : Snap should happening here for things to happen...
		// Lol. Snap Snap Snap... after yeach new better GPS val...
		[SerializeField]
		ARInterface _arInterface;

		[SerializeField]
		ARMapMatching _mapMathching;

		[SerializeField]
		bool _useSnapping;

		[SerializeField]
		float _desiredStartingAccuracy = 5f;

		[SerializeField]
		int _amountOfNodesToCheck;

		[SerializeField]
		int _desiredAccuracy;

		[SerializeField]
		NodeSyncBase[] _syncNodes;

		[SerializeField]
		Transform _player;

		[SerializeField]
		AbstractAlignmentStrategy _alignmentStrategy;

		Location _highestLocation;
		AbstractMap _map;

		public static Action<Location> OnNewHighestAccuracyGPS;

		ARInterface.CustomTrackingState _trackingState;

		public event Action<Alignment> OnAlignmentAvailable;

		void Awake()
		{
			_alignmentStrategy.Register(this);
			_map = LocationProviderFactory.Instance.mapManager;

			// Initialize all sync-nodes.Make them ready to recieve node data.
			// Map needs to be generated before init. Otherwise bunch of errors.

			InitializeSyncNodes();

			_map.OnInitialized += Map_OnInitialized;
		}

		void Map_OnInitialized()
		{
			_map.OnInitialized -= Map_OnInitialized;

			// We don't want location updates until we have a map, otherwise our conversion will fail.
			FirstAlignment();
		}

		protected void FirstAlignment()
		{
			Debug.Log("First Alignment");
			var deviceHeading = LocationProviderFactory.Instance.DefaultLocationProvider.CurrentLocation.DeviceOrientation;

			var position = _map.transform.position;
			_map.transform.SetPositionAndRotation(position, Quaternion.Euler(0, deviceHeading, 0));

			//We want Syncronize to be called when location is updated. This could extend to any other polling methods in the future.
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += SyncronizeNodesToFindAlignment;

			foreach (var node in _syncNodes)
			{
				node.SaveNode();
			}
		}

		void ComputeAlignment()
		{
			// TODO
			// I Like this. Computing aligment here. Though we should throw away computing heading only from nodes..
			// I think as with AR & GPS nodes to MapMatching. We should have the heading from nodes as a input for the final heading.
			// Heading from Nodes, Gyro and Compass. And then calculate. -> Ultra Heading :P

			Debug.Log("Compute Alignment - Start");
			Node currentGpsNode = new Node();
			Node previousGpsNode = new Node();
			Node currentARNode = new Node();
			Node previousARNode = new Node();
			foreach (var syncNode in _syncNodes)
			{
				// HACk to get data from GPS Nodes. 
				if (syncNode.GetType() == typeof(GpsNodeSync))
				{
					var gpsNodes = syncNode.ReturnNodes();
					if (gpsNodes.Length < 2)
					{
						Debug.LogFormat("Not enough ({0})GPS node", gpsNodes.Length);
						return;
					}
					currentGpsNode = syncNode.ReturnLatestNode();
					previousGpsNode = gpsNodes[gpsNodes.Length - 2];
				}
				if (syncNode.GetType() == typeof(ArNodesSync))
				{
					var arNodes = syncNode.ReturnNodes();
					if (arNodes.Length < 2)
					{
						Debug.LogFormat("Not enough ({0})AR node", arNodes.Length);
						return;
					}
					currentARNode = syncNode.ReturnLatestNode();
					previousARNode = arNodes[arNodes.Length - 2];
				}
			}


			var _currentLocationPosition = _map.GeoToWorldPosition(currentGpsNode.LatLon);
			var _previousLocationPosition = _map.GeoToWorldPosition(previousGpsNode.LatLon);

			var _currentArPosition = _map.GeoToWorldPosition(currentARNode.LatLon);
			var _previousArLocation = _map.GeoToWorldPosition(previousARNode.LatLon);

			var _currentAbsoluteGpsVector = _currentLocationPosition - _previousLocationPosition;
			var _currentArVector = _currentArPosition - _previousArLocation;


			var rotation = Vector3.SignedAngle(_currentAbsoluteGpsVector, _currentArVector, Vector3.up);
			var headingQuaternion = Quaternion.Euler(0, rotation, 0);
			var relativeGpsVector = headingQuaternion * _currentAbsoluteGpsVector;

			var _rotation = rotation;

			var accuracy = currentGpsNode.Accuracy;
			var delta = _currentArVector - relativeGpsVector;
			var deltaDistance = delta.magnitude;

			float bias = 0.25f;
			//SynchronizationBias;
			//if (UseAutomaticSynchronizationBias && _count > 2)
			//{
			//	// FIXME: This works fine, but a better approach would be to reset only after we favor GPS.
			//	// In other words, don't reset every time we add a node.
			//	// Generally speaking, this will slowly shift the bias up before resetting bias to 0.
			//	bias = Mathf.Clamp01((.5f * (deltaDistance + ArTrustRange - accuracy)) / deltaDistance);
			//}

			// Our new "origin" will be the difference offset between our last nodes (mapped into the same coordinate space).
			var originOffset = _previousArLocation - headingQuaternion * _previousLocationPosition;

			// Add the weighted delta.
			var _position = (_currentLocationPosition * (1 - bias)) + (_currentArVector * bias);

			//_rotation = _gpsNodes[_count - 1].Heading;
			//_position = _gpsPositions[_count - 1];


#if UNITY_EDITOR
			Debug.LogFormat(
				"AR Vector:{0} GPS Vector:{1} HEADING:{2} HDOP:{3} Relative GPS Vector:{4} BIAS:{5} DISTANCE:{6} OFFSET:{7} BIASED DELTA:{8} OFFSET:{8}"
				, _currentArVector
				, _currentAbsoluteGpsVector
				, rotation
				, accuracy
				, relativeGpsVector
				, bias
				, deltaDistance
				, originOffset
				, delta * bias
				, _position
			);
#endif
			Unity.Utilities.Console.Instance.Log(
				string.Format(
					"Offset: {0},\tHeading: {1},\tDisance: {2},\tBias: {3}"
					, _position
					, _rotation
					, deltaDistance
					, bias
				)
				, "orange"
			);

			var alignment = new Alignment();
			alignment.Rotation = _rotation;
			alignment.Position = _position;

			Debug.Log("Alignment complete");
			OnAlignmentAvailable(alignment);
		}

		protected void SyncronizeNodesToFindAlignment(Location location)
		{
			// Our location provider just got a new update.
			// We now ask all our nodes to update and save the most recent node. 
			// Sync Nodes should also update a "Quality/Accuracy" metric.
			// Quality/Accuracy metric will be used to determine whether the node will be considered for the alignment computation or not. 
			Debug.Log("SyncronizeNodesToFindAlignment");

			foreach (var node in _syncNodes)
			{
				node.SaveNode();
			}
			// Compute Alignment 
			ComputeAlignment();
		}

		void InitializeSyncNodes()
		{
			for (int i = 0; i < _syncNodes.Length; i++)
			{
				_syncNodes[i].InitializeNodeBase();
			}
		}

		void CheckTracking()
		{
			var tracking = new ARInterface.CustomTrackingState();
			if (_arInterface.GetTrackingState(ref tracking))
			{
				if (tracking == ARInterface.CustomTrackingState.Good)
				{
					// Blah blah..
				}
			}
		}

		void SaveHighestAccuracy(Location location)
		{
			if (location.Accuracy <= _desiredStartingAccuracy)
			{
				_highestLocation = location;
				_desiredStartingAccuracy = location.Accuracy;

				if (OnNewHighestAccuracyGPS != null)
				{
					OnNewHighestAccuracyGPS(location);
				}

				// TODO:
				// And snap player to there...
			}
		}

		//async void FindBestNodes()
		//{

		//	while (true)
		//	{
		//		foreach (var nodeSync in _nodeSyncs)
		//		{
		//			if (nodeSync.ReturnNodes().Length >= _amountOfNodesToCheck)
		//			{
		//				var average = CheckAverageAccuracy(nodeSync, _amountOfNodesToCheck);

		//				if (average <= _desiredAccuracy)
		//				{
		//					_mapMathching.MapMatchQuery(nodeSync.ReturnNodes());
		//				}
		//			}
		//		}

		//		await Task.Delay(TimeSpan.FromSeconds(10));
		//	}
		//}

		float CheckAverageAccuracy(NodeSyncBase syncBase, int howManyNodes)
		{
			var nodes = syncBase.ReturnNodes();
			float accuracy = 0;

			for (int i = 1; i < howManyNodes; i++)
			{
				accuracy += nodes[nodes.Length - i].Accuracy;
			}

			var average = accuracy / howManyNodes;
			return average;
		}

		void GetMapMatchingCoords(Node[] nodes)
		{
			foreach (var node in nodes)
			{
				Debug.Log("Check lat lon coords: " + node.LatLon);
			}
		}

		//private void Update()
		//{
		//	if (Input.GetKeyDown(KeyCode.Space))
		//	{
		//		_mapMathching.MapMatchQuery(_syncNodes[0].ReturnNodes());
		//	}
		//}

		void SnapMapToNode(Node node)
		{

		}

		// TODO: Check trackingQuality in AR 
		// and snap to GPS nodes if tracking goes bad..
	}
}
