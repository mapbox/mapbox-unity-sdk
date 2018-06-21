using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.Examples
{
	public class CharacterMovement : MonoBehaviour
	{
		public Material[] Materials;
		public Transform Target;
		public Animator CharacterAnimator;
		public float Speed;
		AstronautMouseController _controller;
		void Start()
		{
			_controller = GetComponent<AstronautMouseController>();
		}

		void Update()
		{
			
			if (_controller.enabled)// Because the mouse control script interferes with this script
			{
				return;
			}

			foreach (var item in Materials)
			{
				item.SetVector("_CharacterPosition", transform.position);
			}

			var distance = Vector3.Distance(transform.position, Target.position);
			if (distance > 0.1f)
			{
				transform.LookAt(Target.position);
				transform.Translate(Vector3.forward * Speed);
				CharacterAnimator.SetBool("IsWalking", true);
			}
			else
			{
				CharacterAnimator.SetBool("IsWalking", false);
			}
		}
	}
}