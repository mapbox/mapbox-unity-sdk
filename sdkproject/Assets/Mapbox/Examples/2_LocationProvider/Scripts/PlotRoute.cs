namespace Mapbox.Examples
{
	using UnityEngine;

	[RequireComponent(typeof(LineRenderer))]
	public class PlotRoute : MonoBehaviour
	{
		[SerializeField]
		Transform _target;

		[SerializeField]
		Color _color;

		[SerializeField]
		float _height;

		[SerializeField]
		float _lineWidth = .2f;

		[SerializeField]
		float _updateInterval;

		[SerializeField]
		float _minDistance;

		LineRenderer _lineRenderer;
		float _elapsedTime;
		int _currentIndex = 0;
		float _sqDistance;
		Vector3 _lastPosition;
		bool _isStable;

		public Vector3[] Positions
		{
			get
			{
				var positions = new Vector3[_lineRenderer.positionCount];
				_lineRenderer.GetPositions(positions);
				return positions;
			}
		}

		void Awake()
		{
			_lineRenderer = GetComponent<LineRenderer>();
			_lineRenderer.startColor = _color;
			_lineRenderer.endColor = _color;
			_lineRenderer.widthMultiplier = _lineWidth;
			_sqDistance = _minDistance * _minDistance;
		}

		void Update()
		{
			_elapsedTime += Time.deltaTime;
			var offset = _target.localPosition - _lastPosition;
			offset.y = 0;

			if (_elapsedTime > _updateInterval && offset.sqrMagnitude > _sqDistance)
			{
				_elapsedTime = 0f;
				AddNode(_target.localPosition);
			}
		}

		void AddNode(Vector3 position)
		{
			if (_height > 0)
			{
				position.y = _height;
			}

			_currentIndex++;
			_lineRenderer.positionCount = _currentIndex;
			_lineRenderer.SetPosition(_currentIndex - 1, position);
			_lastPosition = position;
		}
	}
}