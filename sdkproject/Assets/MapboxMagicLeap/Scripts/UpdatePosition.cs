namespace Mapbox.Examples.MagicLeap
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public class UpdatePosition : MonoBehaviour
	{

		public Transform positionToTrack;

		// Update is called once per frame
		void Update()
		{

			transform.localPosition = positionToTrack.position;

		}
	}
}
