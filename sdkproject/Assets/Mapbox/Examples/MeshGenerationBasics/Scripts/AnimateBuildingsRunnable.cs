namespace Mapbox.Examples
{
	using System.Collections;
	using UnityEngine;
	using Mapbox.Unity.Utilities;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Animate Buildings")]
	public class AnimateBuildingsRunnable : SingletonScriptableObject<AnimateBuildingsRunnable>
	{
		[SerializeField]
		AnimationCurve _curve;

		float GetRandomTime()
		{
			return Random.Range(.01f, 1f);
		}

		public IEnumerator Run(MonoBehaviour runner)
		{
			var transform = runner.transform;

			transform.localScale = new Vector3(1, 0.01f, 1);
			yield return new WaitForSeconds(GetRandomTime());

			var duration = 1f;
			var elapsedTime = 0f;
			while (elapsedTime < duration)
			{
				yield return null;
				elapsedTime += Time.deltaTime;
				var height = _curve.Evaluate(elapsedTime / duration);
				transform.localScale = new Vector3(1, height, 1);
			}
		}
	}
}