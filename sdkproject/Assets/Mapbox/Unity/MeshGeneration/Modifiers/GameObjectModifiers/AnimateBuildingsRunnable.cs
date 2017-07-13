namespace Mapbox.Unity.MeshGeneration.Modifiers
{
	using System.Collections;
	using System.Linq;
	using UnityEngine;

	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Animate Buildings")]
	public class AnimateBuildingsRunnable : ScriptableObjectRunnable
	{
		[SerializeField]
		AnimationCurve _curve;

		static AnimateBuildingsRunnable _instance;
		public static AnimateBuildingsRunnable Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = Resources.FindObjectsOfTypeAll<AnimateBuildingsRunnable>().FirstOrDefault();
				}
				return _instance;
			}
		}

		float GetRandomTime()
		{
			return Random.Range(.01f, 1f);
		}

		public override IEnumerator Run(MonoBehaviour runner)
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