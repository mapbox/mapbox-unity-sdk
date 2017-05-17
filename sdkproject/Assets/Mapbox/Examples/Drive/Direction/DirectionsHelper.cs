using Mapbox.Unity.Map;
namespace Mapbox.Examples.Drive
{
	using UnityEngine;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration;
	using Mapbox.Unity;

	public class DirectionsHelper : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		DirectionsFactory Directions;

		[SerializeField]
		Transform[] _waypoints;

		// TODO: finish
		void Awake()
		{
			//// draw directions path at start
			//_map.WorldCreated += (s, e) =>
			//{
			//	Query();
			//};
		}

		public void Query()
		{
			Directions.Query(_waypoints);
		}
	}
}