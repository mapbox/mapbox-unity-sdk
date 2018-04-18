namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.Collections;
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;
	using Mapbox.Unity.MeshGeneration.Filters;

	[Serializable]
	public class PrefabItemOptions : VectorSubLayerProperties
	{
		#region Fixed Properties
		//Fixed primitiveType
		public readonly VectorPrimitiveType primitiveType = VectorPrimitiveType.Point;

		//Group features turned off
		public readonly bool groupFeatures = false;

		//No extrusion
		public readonly ExtrusionType extrusionType = ExtrusionType.None;

		//Dictionary containing the layer names for each location prefab find by type
		public readonly Dictionary<LocationPrefabFindBy, string> layerNameFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, "poi_label"},
			{LocationPrefabFindBy.POIName, "poi_label"},
		};

		//Dictionary containing the property names in the layer for each location prefab find by type
		public readonly Dictionary<LocationPrefabFindBy, string> categoryPropertyFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, "maki"},
			{LocationPrefabFindBy.POIName, "name"},
		};

		//Dictionary containing the density names in the layer for each location prefab find by type
		public readonly Dictionary<LocationPrefabFindBy, string> densityPropertyFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, "localrank"},
			{LocationPrefabFindBy.POIName, "localrank"},
		};

		//Dictionary containing the density names in the layer for each location prefab find by type
		public readonly Dictionary<LocationPrefabFindBy, string> namePropertyFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, ""},
			{LocationPrefabFindBy.POIName, "name"},
		};

		//Force Move prefab feature position to the first vertex
		public readonly PositionTargetType _movePrefabFeaturePositionTo = PositionTargetType.FirstVertex;
		#endregion

		#region User Choice Propeerties

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Mapbox.Unity.Map.PrefabItemOptions"/> item is active.
		/// </summary>
		/// <value><c>true</c> if is active; otherwise, <c>false</c>.</value>
		public bool isActive
		{
			get
			{
				return coreOptions.isActive;
			}
			set
			{
				coreOptions.isActive = value;
			}
		}

		public bool snapToTerrain
		{
			get
			{
				return coreOptions.snapToTerrain;
			}
			set
			{
				coreOptions.snapToTerrain = value;
			}
		}

		public string prefabItemName
		{
			get
			{
				return coreOptions.sublayerName;
			}
			set
			{
				coreOptions.sublayerName = value;
			}
		}

		/// <summary>
		/// The prefab to be spawned on the map
		/// </summary>
		public SpawnPrefabOptions spawnPrefabOptions;

		/// <summary>
		/// The FindbyType enum to specify the type of prefanb item in the list
		/// </summary>
		public LocationPrefabFindBy findByType = LocationPrefabFindBy.MapboxCategory;//default to Mapbox Category

		/// <summary>
		/// The category filter for the location marker
		/// </summary>
		public LocationPrefabCategories categories;

		/// <summary>
		/// All the POIs containing this name will be shown
		/// </summary>
		public string poiName = "POI Name";

		/// <summary>
		/// The density of pois in an area
		/// </summary>
		[Range(1, 30)]
		public int density = 15;

		#endregion
	}
}