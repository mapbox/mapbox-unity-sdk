using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Utilities;

public class MapFromCode : MonoBehaviour
{
	AbstractMap _map;


	public void SetUpMap()
	{
		// Outlining the process to create a map in code. 
		// Aim here is to understand the limitations of user facing API and de-couple into a MVC'ish paradigm. 


		var flatTerrainFactory = new FlatTerrainFactory();

		// All fields are private. No way to customize the options in code. 
		// The Factories should _not_ have any data components inside of them.
		// Factories should not be ScriptableObjects. 
		// They should consume a data-structure(probably ScriptableObjects) containing options for object creation.
		// Suggested workflow :

		// var terrainOptions = new TerrainOptions();
		// terrainOptions.type = TerrainTypes.Flat;
		// terrainOptions.baseMaterial = new Material();
		// terrainOptions.createSideWalls = false;
		// terrianOptions.sideWallHeight = 20;

		// Supply the options to the factory for object creation.	
		// var flatTerrainFactory = new TerrainFctory(terrainOptions);



		var basicVisualizer = new MapVisualizer();
		// The Factories should _not_ have any data components inside of them.
		// Factories should not be ScriptableObjects. 

		// Should have a method to add/remove factories, not just plain wrappers for list.Add but special intuitive ones
		// Wrapper methods to set/override Raster/Terrain/Vector visualization factories. 
		basicVisualizer.Factories.Add(flatTerrainFactory);

		// Loading Texture is private, can't setup in code. 
		// Using options data-structures is an alternative. Serailizable Data Containers like Scriptable objects can be used. 
		// basicVisualizer.LoadingTexture = some Texture2D;

		var rangeTileProvider = new RangeTileProvider();

		// This will not work currently because AbstractTileProvider is a Monobehaviour, but why exactly?
		// I don't see a reason for this to be a Monobehaviour. Could this be changed to a regular class or ScriptableObject?

		// Suggested workflow :
		//
		// var tileProviderOptions = new RangeTileProviderOptions();
		// tileProviderOptions.SetRange(new Range(n,w,e,s);
		// rangeTileProvider = new TileProvider(tileProviderOptions);

		//_map = gameObject.AddComponent<BasicMap>();

		//_map.MapVisualizer = basicVisualizer;
		//_map.TileProvider = rangeTileProvider;

		_map.Initialize(new Mapbox.Utils.Vector2d(0, 0), 15);

	}

	void SetUpDefaultMap()
	{
		_map = gameObject.AddComponent<AbstractMap>();
		_map.Initialize(new Mapbox.Utils.Vector2d(37.7749, -122.4194), 15);
	}


	void SetupMapWithImageChanges()
	{
		_map = gameObject.AddComponent<AbstractMap>();
		_map.ImageLayer.SetLayerSource(ImagerySourceType.MapboxDark);

		_map.Initialize(new Mapbox.Utils.Vector2d(37.7749, -122.4194), 15);
	}

	void SetupMapWithElevationChanges()
	{
		_map = gameObject.AddComponent<AbstractMap>();
		_map.Terrain.SetLayerSource(ElevationSourceType.MapboxTerrain);

		ElevationRequiredOptions requiredOptions = new ElevationRequiredOptions();
		requiredOptions.baseMaterial = Resources.Load("TerrainMaterial", typeof(Material)) as Material; ;
		_map.Terrain.SetTerrainOptions(ElevationLayerType.TerrainWithElevation, requiredOptions);

		_map.Initialize(new Mapbox.Utils.Vector2d(37.7749, -122.4194), 15);
	}

	// Use this for initialization
	void Start()
	{
		//_map = new BasicMap();
		//SetupMapWithImageChanges();
		SetupMapWithElevationChanges();
	}

	// Update is called once per frame
	void Update()
	{

	}
}
