namespace Mapbox.Unity.Ar
{
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.Location;
	using System;


	public class SimpleAutomaticSynchronizationContext : ISynchronizationContext
	{
		float _rotation;
		Vector3 _position;

		// These are lists for future implementation of averaging/iterating over time/distance.
		List<Location> _gpsNodes = new List<Location>();
		List<Vector3> _gpsPositions = new List<Vector3>();
		List<Vector3> _arNodes = new List<Vector3>();

		int _count;

		Vector3 _currentArVector;
		Vector3 _currentAbsoluteGpsVector;

		Vector3 _previousArNode;
		Vector3 _previousLocationPosition;

		/// <summary>
		/// The synchronization bias.
		/// 0 represents full trust in AR, but no trust in GPS.
		/// 1 represents full trust in GPS position, but no trust in AR.
		/// </summary>
		[Tooltip("The synchronization bias. 0 represents full trust in AR, but no trust in GPS. 1 represents full trust in GPS position, but no trust in AR.")]
		public float SynchronizationBias = 1f;

		/// <summary>
		/// The minimum distance that BOTH gps and ar delta vectors (since last nodes added)
		/// must differ before new nodes can be added.
		/// This is to prevent micromovements in AR from being registered if GPS updates wildly for some reason.
		/// </summary>
		[Tooltip("The minimum distance that BOTH gps and ar delta vectors (since last nodes added) must differ before new nodes can be added. This is to prevent micromovements in AR from being registered if GPS updates wildly for some reason.")]
		public float MinimumDeltaDistance;

		/// <summary>
		/// Use automatic synchronization bias.
		/// This will use ArTrustRange and Location Accuracy to determine bias.
		/// </summary>
		[Tooltip("Use automatic synchronization bias. This will use ArTrustRange and Location Accuracy to determine bias.")]
		public bool UseAutomaticSynchronizationBias;

		/// <summary>
		/// The AR trust radius. Essentially, this is how far we will trust AR to report accurate postions.
		/// AR is great for local position changes, but poor over great distance.
		/// As a node approaches the radius, GPS will generally be favored (assuming location accuracy is high).
		/// </summary>
		[Tooltip("The AR trust radius. Essentially, this is how far we will trust AR to report accurate postions. AR is great for local position changes, but poor over great distance. As a node approaches the radius, GPS will generally be favored (assuming location accuracy is high).")]
		public float ArTrustRange;

		/// <summary>
		/// A proposed alignment has become available. 
		/// This is an event to support future implementations where alignment is calculate over time.
		/// </summary>
		public event Action<Alignment> OnAlignmentAvailable;

		/// <summary>
		/// Add GPS and AR nodes to the context.
		/// Will attempt to compute an alignment.
		/// </summary>
		/// <param name="gpsNode">Gps node.</param>
		/// <param name="arNode">Ar node.</param>
		public void AddSynchronizationNodes(Location location, Vector3 locationPosition, Vector3 arNode)
		{
			_gpsNodes.Add(location);
			_gpsPositions.Add(locationPosition);
			_arNodes.Add(arNode);

			_count = _arNodes.Count;
			if (_count > 1)
			{
				_currentArVector = arNode - _previousArNode;
				_currentAbsoluteGpsVector = locationPosition - _previousLocationPosition;

				// TODO: try to use ArTrustRange instead!
				// This would mean no alignment is calculated until the threshold is met.
				// Perhaps more drift, but also more stable?
				if (_currentArVector.magnitude < MinimumDeltaDistance || _currentAbsoluteGpsVector.magnitude < MinimumDeltaDistance)
				{
					Unity.Utilities.Console.Instance.Log("Minimum movement not yet met (arDelta: " + _currentArVector.magnitude + ", gpsDelta: " + _currentAbsoluteGpsVector.magnitude + ")", "red");
					return;
				}

				ComputeAlignment();

				//Compute next alignment relative to current location.
				_previousArNode = arNode;
				_previousLocationPosition = locationPosition;
			}
			else
			{
				//Initialize previous AR / GPS vectors
				_previousArNode = arNode;
				_previousLocationPosition = locationPosition;
			}
		}

		void ComputeAlignment()
		{
			var rotation = Vector3.SignedAngle(_currentAbsoluteGpsVector, _currentArVector, Vector3.up);
			var headingQuaternion = Quaternion.Euler(0, rotation, 0);
			var relativeGpsVector = headingQuaternion * _currentAbsoluteGpsVector;

			_rotation = rotation;

			var accuracy = _gpsNodes[_count - 1].Accuracy;
			var delta = _currentArVector - relativeGpsVector;
			var deltaDistance = delta.magnitude;

			var bias = SynchronizationBias;
			if (UseAutomaticSynchronizationBias && _count > 2)
			{
				// FIXME: This works fine, but a better approach would be to reset only after we favor GPS.
				// In other words, don't reset every time we add a node.
				// Generally speaking, this will slowly shift the bias up before resetting bias to 0.
				bias = Mathf.Clamp01((.5f * (deltaDistance + ArTrustRange - accuracy)) / deltaDistance);
			}

			// Our new "origin" will be the difference offset between our last nodes (mapped into the same coordinate space).
			var originOffset = _previousArNode - headingQuaternion * _previousLocationPosition;

			// Add the weighted delta.
			_position = (delta * bias) + originOffset;

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
				, delta
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

			OnAlignmentAvailable(alignment);
		}
	}
}
