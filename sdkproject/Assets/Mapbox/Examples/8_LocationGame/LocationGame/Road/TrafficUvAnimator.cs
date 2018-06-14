using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficUvAnimator : MonoBehaviour
{
	public Material[] XAxisMaterials;
	public Material[] YAxisMaterials;
	public float Speed;
	private Vector2 _xOffset;
	private Vector2 _yOffset;

	void Start()
	{

	}

	void Update()
	{
		_xOffset.Set(_xOffset.x + Time.deltaTime * Speed, 0.2f);
		_yOffset.Set(0, _yOffset.y + Time.deltaTime * Speed);

		foreach (var item in XAxisMaterials)
		{
			item.SetTextureOffset("_MainTex", _xOffset);
		}

		foreach (var item in YAxisMaterials)
		{
			item.SetTextureOffset("_MainTex", _yOffset);
		}
	}
}
