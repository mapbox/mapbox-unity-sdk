using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
	public Material[] Materials;

	void Start()
	{

	}
	
	void Update()
	{
		foreach (var item in Materials)
		{
			item.SetVector("_CharacterPosition", transform.position);
		}
	}
}
