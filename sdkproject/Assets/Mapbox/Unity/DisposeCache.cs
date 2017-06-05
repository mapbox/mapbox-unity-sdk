using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisposeCache : MonoBehaviour {

	private void OnApplicationQuit()
	{

		Mapbox.Unity.MapboxAccess.Instance.DisposeCache();
	}
}
