using System.Collections;
using System.Collections.Generic;
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
