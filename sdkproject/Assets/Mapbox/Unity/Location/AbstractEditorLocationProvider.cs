namespace Mapbox.Unity.Location
{
	using System.Collections;
	using UnityEngine;

	public abstract class AbstractEditorLocationProvider : AbstractLocationProvider
	{
		[SerializeField]
		protected int _accuracy;

		[SerializeField]
		bool _autoFireEvent;

		[SerializeField]
		float _updateInterval;

		[SerializeField]
		bool _sendEvent;

		WaitForSeconds _wait;

#if UNITY_EDITOR
		protected virtual void Awake()
		{
			_wait = new WaitForSeconds(_updateInterval);
			StartCoroutine(QueryLocation());
		}
#endif

		IEnumerator QueryLocation()
		{
			while (true)
			{
				SetLocation();
				if (_autoFireEvent)
				{
					SendLocation(_currentLocation);
				}
				yield return _wait;
			}
		}

		void OnValidate()
		{
			if (_sendEvent)
			{
				_sendEvent = false;
				SendLocation(_currentLocation);
			}
		}

		protected abstract void SetLocation();
	}
}