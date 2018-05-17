using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficUvAnimator : MonoBehaviour
{
	public Material[] Materials;
	public float Speed;
	private Vector2 _offset;

	void Start()
	{

	}

	void Update()
	{
		_offset.Set(0, _offset.y + Time.deltaTime * Speed);
		foreach (var item in Materials)
		{
			item.SetTextureOffset("_MainTex", _offset);
		}
	}
}
