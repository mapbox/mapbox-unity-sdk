using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.Examples
{
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
			_offset.Set(_offset.x + Time.deltaTime * Speed, 0.2f);

			foreach (var item in Materials)
			{
				item.SetTextureOffset("_MainTex", _offset);
			}
		}
	}
}

