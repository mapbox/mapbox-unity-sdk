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
			// HACK: Let others register before we send our first event. 
			// Often this happens in Start.
			yield return new WaitForSeconds(.1f);
			while (true)
			{
				if (_autoFireEvent)
				{
					SendLocationEvent();
				}
				yield return _wait;
			}
		}


		// Added to support TouchCamera script. 
		public void SendLocationEvent()
		{
			SetLocation();
			SendLocation(_currentLocation);
		}


		void OnValidate()
		{
			if (_sendEvent)
			{
				_sendEvent = false;
				SendLocationEvent();
			}
		}

		protected abstract void SetLocation();
	}
}
