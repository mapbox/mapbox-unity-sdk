using Mapbox.Unity.MeshGeneration.Modifiers;
using UnityEngine;

public class AnimateBuildings : MonoBehaviour
{
	public ScriptableObjectRunnable _runnable;

	void Awake()
	{
		_runnable = AnimateBuildingsRunnable.Instance;
		StartCoroutine(_runnable.Run(this));
	}
}