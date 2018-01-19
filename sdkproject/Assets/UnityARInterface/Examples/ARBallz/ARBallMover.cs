using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

namespace UnityARInterface
{
	public class ARBallMover : ARBase {

		public float maxForce = 10.0f;
		public float maxSqrMagnitude = 0.1f;
		private GameObject collBallGO;

		public bool MoverActive { get; private set; }
		public Vector3 MoverPosition { get; private set; }

		// Use this for initialization
		void Start () {
			MoverActive = false;
		}


		// Update is called once per frame
		void Update () {
			if (!isActiveAndEnabled)
				return;

			MoverActive = false;
			if (Input.GetMouseButton (0)) {
				var camera = GetCamera ();

				Ray ray = camera.ScreenPointToRay (Input.mousePosition);

				int layerMask = 1 << LayerMask.NameToLayer ("ARGameObject"); //Planes are in layer ARGameObject

				RaycastHit rayHit;
				if (Physics.Raycast (ray, out rayHit, float.MaxValue, layerMask)) {
					MoverPosition = rayHit.point;
					MoverActive = true;
				}
			} 
		}
	}
}