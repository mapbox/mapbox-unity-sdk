using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
	public class ARBallzGO : MonoBehaviour {

		public float yDistanceThreshold;

		private float startingY;
		public ARBallMover BallMover { get; set; }

		private Rigidbody rigidBody;

		// Use this for initialization
		void Start () {
			startingY = transform.position.y;
			rigidBody = GetComponent<Rigidbody> ();
		}


		// Update is called once per frame
		void Update () {

			if (Mathf.Abs (startingY - transform.position.y) > yDistanceThreshold) {
				Destroy (gameObject);
			}

			if (BallMover != null && BallMover.MoverActive && rigidBody != null) {
				//calculate force based on distance sqaured
				Vector3 fromMover = transform.position - BallMover.MoverPosition;
				float sqrMagnitude = fromMover.sqrMagnitude;
				Vector3 normForceVector = fromMover.normalized;
				if (sqrMagnitude < BallMover.maxSqrMagnitude && sqrMagnitude > 0.01f)
				{
					float forceMag = BallMover.maxForce / sqrMagnitude;
					rigidBody.AddForce (normForceVector * forceMag);
				}
			}
		}
	}
}