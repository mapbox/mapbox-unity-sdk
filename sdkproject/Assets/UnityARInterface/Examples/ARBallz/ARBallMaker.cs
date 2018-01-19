using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

namespace UnityARInterface
{
	public class ARBallMaker : ARBase {

		public GameObject ballPrefab;
		public float createHeight;
		public ARBallMover ballMover;
		private MaterialPropertyBlock props;
		private bool firstClick = true;

		// Use this for initialization
		void Start () {
			props = new MaterialPropertyBlock ();

		}

		void CreateBall(Vector3 atPosition)
		{
			GameObject ballGO = Instantiate (ballPrefab, atPosition, Quaternion.identity);
				
			
			float r = Random.Range(0.0f, 1.0f);
			float g = Random.Range(0.0f, 1.0f);
			float b = Random.Range(0.0f, 1.0f);

			props.SetColor("_InstanceColor", new Color(r, g, b));

			MeshRenderer renderer = ballGO.GetComponent<MeshRenderer>();
			renderer.SetPropertyBlock(props);

			ARBallzGO arBallzGO = ballGO.GetComponent<ARBallzGO> ();
			arBallzGO.BallMover = ballMover;
		}

		// Update is called once per frame
		void Update () {
			if (!isActiveAndEnabled)
				return;
			
			if (Input.GetMouseButton (0)) {
				if (firstClick) {
					var camera = GetCamera ();

					Ray ray = camera.ScreenPointToRay (Input.mousePosition);

					int layerMask = 1 << LayerMask.NameToLayer ("ARGameObject"); // Planes are in layer ARGameObject

					RaycastHit rayHit;
					if (Physics.Raycast (ray, out rayHit, float.MaxValue, layerMask)) {
						Vector3 position = rayHit.point;
						CreateBall (new Vector3 (position.x, position.y + createHeight, position.z));
					}
					firstClick = false;
				}
			} else {
				firstClick = true;
			}

		}

	}
}