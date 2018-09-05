namespace Mapbox.Examples.MagicLeap
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public class ControllerLabelLine : MonoBehaviour
	{

		[SerializeField]
		private Transform _label;
		[SerializeField]
		private Transform _controllerComponent;

		private LineRenderer _lineRenderer;

		// Use this for initialization
		void Start()
		{

			_lineRenderer = GetComponent<LineRenderer>();
			_lineRenderer.positionCount = 2;
			_lineRenderer.useWorldSpace = true;

		}

		// Update is called once per frame
		void Update()
		{

			_lineRenderer.SetPosition(0, _controllerComponent.position);
			_lineRenderer.SetPosition(1, _label.position);
			_lineRenderer.enabled = true;

		}

		private void OnDisable()
		{
			_lineRenderer.enabled = false;
		}
	}
}
