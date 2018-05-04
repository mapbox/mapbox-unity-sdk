namespace Mapbox.Unity.Ar
{
	using UnityEngine;
	using System;

	//using System.Threading.Tasks;
	public class GPSARLinearInterpolationLocalizationStrategy : ComputeARLocalizationStrategy
	{
		[SerializeField]
		[Range(0, 1)]
		float _bias = 1.0f;
		public override event Action<Alignment> OnLocalizationComplete;

		/// <summary>
		/// Computes the localization using GPS and AR nodes. 
		/// Uses a bias to compute the position and roation from the GSP and AR locations. 
		/// </summary>
		/// <param name="centralizedARLocator">Centralized ARL ocator.</param>
		public override void ComputeLocalization(CentralizedARLocator centralizedARLocator)
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
			foreach (var syncNode in centralizedARLocator.SyncNodes)
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
					previousGpsNode = gpsNodes[1];
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
					previousARNode = arNodes[1];
				}
			}


			var _currentLocationPosition = centralizedARLocator.CurrentMap.GeoToWorldPosition(currentGpsNode.LatLon);
			var _previousLocationPosition = centralizedARLocator.CurrentMap.GeoToWorldPosition(previousGpsNode.LatLon);

			var _currentArPosition = centralizedARLocator.CurrentMap.GeoToWorldPosition(currentARNode.LatLon);
			var _previousArLocation = centralizedARLocator.CurrentMap.GeoToWorldPosition(previousARNode.LatLon);

			var _currentAbsoluteGpsVector = _currentLocationPosition - _previousLocationPosition;
			var _currentArVector = _currentArPosition - _previousArLocation;


			var rotation = Vector3.SignedAngle(_currentAbsoluteGpsVector, _currentArVector, Vector3.up);
			var headingQuaternion = Quaternion.Euler(0, rotation, 0);
			var relativeGpsVector = headingQuaternion * _currentAbsoluteGpsVector;

			var _rotation = rotation;

			var accuracy = currentGpsNode.Accuracy;
			var delta = _currentArVector - relativeGpsVector;
			var deltaDistance = delta.magnitude;


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
			var _position = (_currentLocationPosition * (1 - _bias)) + (_currentArVector * _bias);

			//_rotation = _gpsNodes[_count - 1].Heading;
			//_position = _gpsPositions[_count - 1];


#if UNITY_EDITOR
			Debug.LogFormat(
				"AR Vector:{0} GPS Vector:{1} HEADING:{2} HDOP:{3} Relative GPS Vector:{4} BIAS:{5} DISTANCE:{6} OFFSET:{7} BIASED DELTA:{8} OFFSET:{8}"
				, _currentArVector
				, _currentAbsoluteGpsVector
				, _rotation
				, accuracy
				, relativeGpsVector
				, _bias
				, deltaDistance
				, originOffset
				, delta * _bias
				, _position
			);
#endif
			Unity.Utilities.Console.Instance.Log(
				string.Format(
					"Offset: {0},\tHeading: {1},\tDisance: {2},\tBias: {3}"
					, _position
					, _rotation
					, deltaDistance
					, _bias
				)
				, "orange"
			);

			var alignment = new Alignment();
			alignment.Rotation = _rotation;
			alignment.Position = _position;

			Debug.Log("Alignment complete");
			OnLocalizationComplete(alignment);
		}
	}
}
