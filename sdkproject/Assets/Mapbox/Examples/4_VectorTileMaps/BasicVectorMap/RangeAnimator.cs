using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeAnimator : MonoBehaviour
{
	private MeshRenderer _meshRenderer;
	private Color _org;
	//public static Color InRangeColor = new Color(205,0.31f,0);
	private const string EmissiveColor = "_EmissionColor";
	private bool _skip = false;

	public void Initialize(MeshRenderer mr)
	{
		_meshRenderer = mr;
		_org = _meshRenderer.material.color;
	}

	public void LateUpdate()
	{
		if (_meshRenderer.material.color == _org || _skip)
		{
			_skip = false;
			return;
		}

		//_meshRenderer.material.color = Color.Lerp(_meshRenderer.material.color, _org, Time.deltaTime * 5);
		//_meshRenderer.material.SetColor(EmissiveColor, Color.black);
	}

	public void InRange(Color c, double r)
	{
		_skip = true;
		_meshRenderer.material.color = Color.Lerp(_org, c, (float)r);
		//_meshRenderer.material.SetColor(EmissiveColor, Color.Lerp(_org, InRangeColor, (float)r));
	}
}
