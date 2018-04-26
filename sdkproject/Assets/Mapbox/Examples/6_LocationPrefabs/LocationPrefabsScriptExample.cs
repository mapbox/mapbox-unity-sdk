namespace Mapbox.Examples
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using Mapbox.Unity.Map;
	using Mapbox.Utils;

	public class LocationPrefabScriptExample : MonoBehaviour
	{

		public AbstractMap map;
		public GameObject myCustomPrefab;

		// Use this for initialization
		void Start()
		{
			map.OnInitialized += HandleMapInitialized;
			map.MapVisualizer.OnMapVisualizerStateChanged += HandleMapStateChange;

			//add layers before initialize
			map.SpawnPrefabAtGeoLocation(myCustomPrefab, new Vector2d(37.784179, -122.401583), HandlePrefabsPlaced);
			map.Initialize(new Vector2d(37.784179, -122.401583), 16);
		}

		// Update is called once per frame
		void HandleMapInitialized()
		{
			//add layers on initialize

		}

		void HandleMapStateChange(ModuleState state)
		{
			if (!(state == ModuleState.Finished))
			{
				//add layers on loaded
				return;
			}
		}

		//handle callbacks
		void HandlePrefabsPlaced(List<GameObject> instances)
		{
			Debug.Log(instances.Count);
		}
	}
}
