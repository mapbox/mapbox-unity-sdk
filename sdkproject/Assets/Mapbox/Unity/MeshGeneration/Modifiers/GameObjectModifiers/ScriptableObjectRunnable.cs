namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections;
	using UnityEngine;

	public abstract class ScriptableObjectRunnable : ScriptableObject
	{
		public abstract IEnumerator Run(MonoBehaviour runner);
	}
}