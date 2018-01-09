namespace Mapbox.Unity.Utilities
{
	using UnityEngine;

	public class DontDestroyOnLoad : MonoBehaviour
	{
		static DontDestroyOnLoad _instance;

		[SerializeField]
		bool _useSingleInstance;

		void Awake()
		{
			if (_instance != null && _useSingleInstance)
			{
				Destroy(gameObject);
				return;
			}

			_instance = this;
			DontDestroyOnLoad(gameObject);
		}
	}
}