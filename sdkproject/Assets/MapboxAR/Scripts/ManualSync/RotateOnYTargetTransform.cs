namespace Mapbox.Examples
{
	using UnityEngine;

	public class RotateOnYTargetTransform : MonoBehaviour
	{
		[SerializeField]
		Transform _targetTransform;

		void Update()
		{
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, _targetTransform.eulerAngles.y, transform.eulerAngles.z);
		}
	}
}
