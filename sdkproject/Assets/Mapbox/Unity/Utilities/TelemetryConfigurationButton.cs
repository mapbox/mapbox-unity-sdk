namespace Mapbox.Unity.Utilities
{
	using UnityEngine;
	using UnityEngine.UI;

	[RequireComponent(typeof(Button))]
	public class TelemetryConfigurationButton : MonoBehaviour
	{
		[SerializeField]
		bool _booleanValue;

		void Awake()
		{
			GetComponent<Button>().onClick.AddListener(SetPlayerPref);
		}

		void SetPlayerPref()
		{
			PlayerPrefs.SetInt(Constants.Path.SHOULD_COLLECT_LOCATION_KEY, (_booleanValue ? 1 : 0));
			MapboxAccess.Instance.SetLocationCollectionState(_booleanValue);
			PlayerPrefs.Save();
		}
	}
}