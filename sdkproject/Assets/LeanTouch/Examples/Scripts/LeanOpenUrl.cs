using UnityEngine;

namespace Lean.Touch
{
	public class LeanOpenUrl : MonoBehaviour
	{
		public void Open(string url)
		{
			Application.OpenURL(url);
		}
	}
}