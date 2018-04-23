namespace Mapbox.Unity.Ar
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Location;

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

		void Start()
		{
			//TODO : This needs to have InitializdedARMode notifier.
			//That is notified on ARPlanePlacement....
			//_map.Initialize();
			//OOOOR actually this should start running from CentralizedArLocator...
			// NodeSyncBase call that calls for Run!

			_waitFor = new WaitForSeconds(_secondsBetweenDropCheck);
			_savedNodes = new List<Node>();
			CentralizedARLocator.OnNewHighestAccuracyGPS += SavedGPSAccuracy;

			//Then you wont need this fucking crazy shenanigans...
			Action handler = null;
			handler = () =>
			{
				StartCoroutine(SaveArNodes(_targetTransform));
				_map.OnInitialized -= handler;
			};
			_map.OnInitialized += handler;
		}

		void SavedGPSAccuracy(Location location)
		{
			_latestBestGPSAccuracy = location.Accuracy;
		}

		IEnumerator SaveArNodes(Transform dropTransform)
		{
			while (true)
			{
				ConvertToNodes(dropTransform);
				yield return _waitFor;
			}
		}

		void ConvertToNodes(Transform nodeDrop)
		{
			// Despise if else jungles...
			if (_savedNodes.Count >= 1)
			{
				var previousNodePos = _map.GeoToWorldPosition(_savedNodes[_savedNodes.Count - 1].LatLon, false);
				var currentMagnitude = nodeDrop.position - previousNodePos;

				if (currentMagnitude.magnitude >= _minMagnitudeBetween)
				{
					var node = new Node();
					node.LatLon = _map.WorldToGeoPosition(nodeDrop.position);
					node.Accuracy = _latestBestGPSAccuracy;
					_savedNodes.Add(node);

					if (NodeAdded != null)
					{
						NodeAdded();
					}
				}
			}
			else
			{
				var node = new Node();
				node.LatLon = _map.WorldToGeoPosition(nodeDrop.position);
				node.Accuracy = _latestBestGPSAccuracy;
				_savedNodes.Add(node);

				if (NodeAdded != null)
				{
					NodeAdded();
				}
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

