namespace Mapbox.Examples
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public class CameraMovementMockup : MonoBehaviour
	{
		public Transform Target;
		public float Speed;

		void Start()
		{

		}

		void Update()
		{
			if (Vector3.Distance(transform.position, Target.position) > 0.1f)
			{
				transform.LookAt(Target.position);
				transform.Translate(Vector3.forward * Speed);
			}
		}
	}
}
