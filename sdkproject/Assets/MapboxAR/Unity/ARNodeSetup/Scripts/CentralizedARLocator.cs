namespace Mapbox.Unity.Ar
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Location;
	using System;
	using Mapbox.MapMatching;
	using Mapbox.Utils;
	using System.Threading.Tasks;
	using UnityARInterface;

	public class CentralizedARLocator : MonoBehaviour
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
		NodeSyncBase[] _nodeSyncs;

		[SerializeField]
		Transform _player;

		Node[] _nodes;
		Location _highestLocation;

		public static Action<Location> OnNewHighestAccuracyGPS;

		ARInterface.CustomTrackingState _trackingState;

		void Start()
		{
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += SaveHighestAccuracy;
			_mapMathching.ReturnMapMatchCoords += GetMapMatchingCoords;
			Invoke("hack", 5f);
		}

		void hack()
		{
			for (int i = 0; i < _nodeSyncs.Length; i++)
			{
				_nodeSyncs[i].InitializeNodeBase();
			}

			FindBestNodes();
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

		async void FindBestNodes()
		{

			while (true)
			{
				foreach (var nodeSync in _nodeSyncs)
				{
					if (nodeSync.ReturnNodes().Length >= _amountOfNodesToCheck)
					{
						var average = CheckAverageAccuracy(nodeSync, _amountOfNodesToCheck);

						if (average <= _desiredAccuracy)
						{
							_mapMathching.MapMatchQuery(nodeSync.ReturnNodes());
						}
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(10));
			}
		}

		int CheckAverageAccuracy(NodeSyncBase syncBase, int howManyNodes)
		{
			var nodes = syncBase.ReturnNodes();
			int accuracy = 0;

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

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				_mapMathching.MapMatchQuery(_nodeSyncs[0].ReturnNodes());
			}
		}

		void SnapMapToNode(Node node)
		{

		}

		// TODO: Check trackingQuality in AR 
		// and snap to GPS nodes if tracking goes bad..
	}
}
