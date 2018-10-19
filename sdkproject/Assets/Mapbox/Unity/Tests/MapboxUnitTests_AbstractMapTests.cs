namespace Mapbox.Unity.Tests
{
	using Mapbox.Unity.Map;
	using System.Collections;
	using NUnit.Framework;
	using UnityEngine;
	using UnityEngine.TestTools;

	[TestFixture]
	internal class AbstractMapTests
	{
		GameObject _map;

		[UnityTest]
		public IEnumerator SetUpDefaultMap()
		{
			var go = new GameObject("Map");
			var _map = go.AddComponent<AbstractMap>();
			_map.OnInitialized += () =>
			{
				Assert.IsNotNull(_map);
			};

			yield return new WaitForFixedUpdate(); ;
			_map.Initialize(new Mapbox.Utils.Vector2d(37.7749, -122.4194), 15);
		}
	}

}