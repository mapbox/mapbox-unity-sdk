namespace Mapbox.Unity.Ar
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Location;
	using System;
	using Mapbox.MapMatching;
	using Mapbox.Utils;

	public class CentralizedARLocator : MonoBehaviour
	{

		// TODO : Snap should happening here for things to happen...
		// Lol. Snap Snap Snap... after yeach new better GPS val...

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

		IEnumerator _checkNodes;
		WaitForSeconds _waitFor;

		public static Action<Location> OnNewHighestAccuracyGPS;

		void Start()
		{
			LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += SaveHighestAccuracy;
			_mapMathching.ReturnMapMatchCoords += GetMapMatchingCoords;
			_waitFor = new WaitForSeconds(10);
			_checkNodes = FindBestNodes();
			Invoke("hack", 10f);
		}

		void hack()
		{
			StartCoroutine(_checkNodes);
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

		IEnumerator FindBestNodes()
		{

			while (true)
			{
				foreach (var nodeSync in _nodeSyncs)
				{
					var average = CheckAverageAccuracy(nodeSync, _amountOfNodesToCheck);
					if (average <= _desiredAccuracy)
					{
						// TODO: Do map matching based on Nodes.
						_mapMathching.MapMatchQuery(nodeSync.ReturnNodes());
					}
				}

				yield return _waitFor;
			}
		}

		int CheckAverageAccuracy(NodeSyncBase syncBase, int howManyNodes)
		{
			var nodes = syncBase.ReturnNodes();
			int accuracy = 0;

			if (howManyNodes > nodes.Length)
			{
				Debug.Log("Not enough nodes!");
				return 100;
			}

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
