using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
	public Material[] Materials;
	public Transform Target;
	public Animator CharacterAnimator;
	public float Speed;

	void Start()
	{

	}
	
	void Update()
	{
		foreach (var item in Materials)
		{
			item.SetVector("_CharacterPosition", transform.position);
		}

		if (Vector3.Distance(transform.position, Target.position) > 0.1f)
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
