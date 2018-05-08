namespace Mapbox.Unity.Utilities
{
	using UnityEngine;
	using UnityEngine.UI;

	[RequireComponent(typeof(Button))]
	public class OpenUrlOnButtonClick : MonoBehaviour
	{
		[SerializeField]
		string _url;

		protected virtual void Awake()
		{
			GetComponent<Button>().onClick.AddListener(VisitUrl);
		}

		void VisitUrl()
		{
			Application.OpenURL(_url);
		}
	}
}
