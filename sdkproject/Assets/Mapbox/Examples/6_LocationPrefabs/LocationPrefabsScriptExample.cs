namespace Mapbox.Examples
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using Mapbox.Unity.Map;
	using Mapbox.Utils;

	public class LocationPrefabsScriptExample : MonoBehaviour
	{

		public AbstractMap map;

		//prefab to spawn
		public GameObject beforeInitialize;
		//cache of spanwed gameobjects
		private List<GameObject> _beforeInitializeInstances;

		public GameObject afterInitialize;
		private List<GameObject> _afterInitializeInstances;

		public GameObject afterLoaded;
		private List<GameObject> _afterLoadedInstances;





		// Use this for initialization
		void Start()
		{
			map.OnInitialized += HandleMapInitialized;
			map.MapVisualizer.OnMapVisualizerStateChanged += HandleMapStateChange;

			//add layers before initialize
			map.SpawnPrefabByCategory(beforeInitialize, LocationPrefabCategories.ArtsAndEntertainment, 10, HandleBeforeInitializePrefabs, true, "BeforeInitLayer");
			map.Initialize(new Vector2d(37.784179, -122.401583), 16);
		}

		// Update is called once per frame
		void HandleMapInitialized()
		{
			map.SpawnPrefabByCategory(afterInitialize, LocationPrefabCategories.Shops, 10, HandleAfterInitializePrefabs, true, "AfterInitLayer");
			map.UpdateMap(map.CenterLatitudeLongitude, map.InitialZoom);
		}

		void HandleMapStateChange(ModuleState state)
		{
			if (!(state == ModuleState.Finished))
			{
				//add layers on loaded
				map.SpawnPrefabByCategory(afterLoaded, LocationPrefabCategories.Food, 10, HandleAfterLoadedPrefabs, true, "AfterLoadedLayer");
				map.UpdateMap(map.CenterLatitudeLongitude, map.InitialZoom);
				return;
			}
		}

		//handle callbacks
		void HandleBeforeInitializePrefabs(List<GameObject> instances)
		{
			Debug.Log(instances.Count);
			//_beforeInitializeInstances.AddRange(instances);
		}

		void HandleAfterInitializePrefabs(List<GameObject> instances)
		{
			//_afterInitializeInstances.AddRange(instances);
		}

		void HandleAfterLoadedPrefabs(List<GameObject> instances)
		{
			//_afterLoadedInstances.AddRange(instances);
		}
	}
}
