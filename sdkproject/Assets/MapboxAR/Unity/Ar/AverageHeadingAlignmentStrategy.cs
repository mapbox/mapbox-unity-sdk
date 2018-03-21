namespace Mapbox.Unity.Ar
{
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;
	using UnityEngine;

	public class AverageHeadingAlignmentStrategy : AbstractAlignmentStrategy
	{
		[SerializeField]
		int _maxSamples = 5;

		[SerializeField]
		float _ignoreAngleThreshold = 15f;

		[SerializeField]
		float _lerpSpeed;

		List<float> _rotations = new List<float>();

		float _averageRotation;
		Quaternion _targetRotation;
		Vector3 _targetPosition;

		public static float meanAngle(List<float> rotations)
		{
			float xValue = 0, yValue = 0;
			foreach (var r in rotations)
			{
				xValue += Mathf.Cos(r * Mathf.Deg2Rad);
				yValue += Mathf.Sin(r * Mathf.Deg2Rad);
			}

			return Mathf.Rad2Deg * Mathf.Atan2( yValue / rotations.Count, xValue / rotations.Count);
		}

		public override void OnAlignmentAvailable(Alignment alignment)
		{
			var count = _rotations.Count;
			var rotation = alignment.Rotation;

			// TODO: optimize circular list.
			if (count >= _maxSamples)
			{
				_rotations.RemoveAt(0);
			}

			if (rotation < 0)
			{
				rotation += 360;
			}

			_rotations.Add(rotation);
			_averageRotation = meanAngle(_rotations);

			if (Mathf.Abs(Mathf.DeltaAngle(rotation, _averageRotation)) < _ignoreAngleThreshold)
			{
				Console.Instance.Log(string.Format("Average Heading: {0}", _averageRotation), "aqua");
				_targetRotation = Quaternion.Euler(0, _averageRotation, 0);
				_targetPosition = alignment.Position;

				// HACK: Undo the original expected position.
				_targetPosition = Quaternion.Euler(0, -rotation, 0) * _targetPosition;

				// Add our averaged rotation.
				_targetPosition = Quaternion.Euler(0, _averageRotation, 0) * _targetPosition;
			}
			else
			{
				Console.Instance.Log("Ignoring alignment (^) due to poor angle (Alignment rotation: "+rotation+", _averageRotation: "+_averageRotation+ "("+(_averageRotation+360)+"), _ignoreAngleThreshold: " + _ignoreAngleThreshold + ")", "red");
			}
		}

		// FIXME: this should be in a coroutine, which is activated in Align.
		void Update()
		{
			var t = _lerpSpeed * Time.deltaTime;
			_transform.SetPositionAndRotation(
				Vector3.Lerp(_transform.localPosition, _targetPosition, t),
				Quaternion.Lerp(_transform.localRotation, _targetRotation, t));
		}
	}
}
