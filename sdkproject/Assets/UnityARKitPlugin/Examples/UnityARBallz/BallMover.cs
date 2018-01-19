using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class BallMover : MonoBehaviour {

	public GameObject collBallPrefab;
	private GameObject collBallGO;

	// Use this for initialization
	void Start () {
		
	}

	void CreateMoveBall( Vector3 explodePosition)
	{
		collBallGO = Instantiate (collBallPrefab, explodePosition, Quaternion.identity);
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.touchCount > 0 )
		{
			var touch = Input.GetTouch(0);
			if (touch.phase == TouchPhase.Began) {
				var screenPosition = Camera.main.ScreenToViewportPoint (touch.position);
				ARPoint point = new ARPoint {
					x = screenPosition.x,
					y = screenPosition.y
				};

				List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface ().HitTest (point, 
					                                   ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent);
				if (hitResults.Count > 0) {
					foreach (var hitResult in hitResults) {
						Vector3 position = UnityARMatrixOps.GetPosition (hitResult.worldTransform);
						CreateMoveBall (position);
						break;
					}
				}

			} else if (touch.phase == TouchPhase.Moved && collBallGO != null) {
				var screenPosition = Camera.main.ScreenToViewportPoint (touch.position);
				ARPoint point = new ARPoint {
					x = screenPosition.x,
					y = screenPosition.y
				};

				List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface ().HitTest (point, 
					                                   ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent);
				if (hitResults.Count > 0) {
					foreach (var hitResult in hitResults) {
						Vector3 position = UnityARMatrixOps.GetPosition (hitResult.worldTransform);
						collBallGO.transform.position = Vector3.MoveTowards (collBallGO.transform.position, position, 0.05f);
						break;
					}
				}
			} else if (touch.phase != TouchPhase.Stationary) { //ended or cancelled
				Destroy(collBallGO);
				collBallGO = null;

			}
		}

	}
}
