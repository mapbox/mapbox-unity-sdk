namespace Mapbox.Unity.Ar
{
	using UnityEngine;

	public class LerpAlignmentStrategy : AbstractAlignmentStrategy
	{
		[SerializeField]
		float _followFactor;

		Vector3 _targetPosition;
		Quaternion _targetRotation = Quaternion.identity;
		bool _isAlignmentAvailable = false;

		public override void OnAlignmentAvailable(Alignment alignment)
		{
			_targetPosition = alignment.Position;
			_targetRotation = Quaternion.Euler(0, alignment.Rotation, 0);
			_isAlignmentAvailable = true;
		}

		// FIXME: this should be in a coroutine, which is activated in Align.
		void Update()
		{
			if (_isAlignmentAvailable)
			{
				var t = _followFactor * Time.deltaTime;
				_transform.SetPositionAndRotation(
					Vector3.Lerp(_transform.localPosition, _targetPosition, t),
					Quaternion.Lerp(_transform.localRotation, _targetRotation, t));
				_isAlignmentAvailable = false;
			}

		}
	}
}
