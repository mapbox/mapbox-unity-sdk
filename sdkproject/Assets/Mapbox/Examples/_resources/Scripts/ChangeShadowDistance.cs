using UnityEngine;
using System.Collections;

public class ChangeShadowDistance : MonoBehaviour
{
	public int ShadowDistance = 3000;

	void Start()
	{
		QualitySettings.shadowDistance = ShadowDistance;
	}
}
