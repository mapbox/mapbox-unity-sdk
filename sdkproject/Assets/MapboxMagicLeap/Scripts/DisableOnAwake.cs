namespace Mapbox.Examples.MagicLeap
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public class DisableOnAwake : MonoBehaviour
	{

		// Use this for initialization
		void Awake()
		{

			gameObject.SetActive(false);

		}
	}
}
