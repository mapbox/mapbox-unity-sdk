namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.Collections;
	using System;
	using System.Collections.Generic;
	using Mapbox.Unity.MeshGeneration.Modifiers;

	[Serializable]
	public class PrefabItemOptions : VectorSubLayerProperties
	{
		#region Private Fixed Properties
		//Fixed primitiveType
		private VectorPrimitiveType primitiveType = VectorPrimitiveType.Point;

		//Group features turned off
		private bool groupFeatures = false;

		//No extrusion
		private ExtrusionType extrusionType = ExtrusionType.None;

		//Dictionary containing the layer names for each location prefab find by type
		private readonly Dictionary<LocationPrefabFindBy, string> layerNameFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, "poi_label"},
			{LocationPrefabFindBy.POIName, "poi_label"},
		};

		//Dictionary containing the property names in the layer for each location prefab find by type
		private readonly Dictionary<LocationPrefabFindBy, string> propertyNameFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, "maki"},
			{LocationPrefabFindBy.POIName, "name"},
		};

		//Force Move prefab feature position to the first vertex
		private readonly PositionTargetType movePrefabFeaturePositionTo = PositionTargetType.FirstVertex;
		#endregion

		public PrefabItemOptions()
		{
			base.coreOptions.geometryType = VectorPrimitiveType.Point;
			if (findByType != LocationPrefabFindBy.AddressOrLatLon)
			{
				_checkAndAddDefaultLayerAndProperty();
			}

			base.extrusionOptions = new GeometryExtrusionOptions
			{
				extrusionType = extrusionType
			};

			base.coreOptions.groupFeatures = groupFeatures;
			base.moveFeaturePositionTo = moveFeaturePositionTo;
		}


		void _checkAndAddDefaultLayerAndProperty()
		{
			var layerName = "";
			if (layerNameFromFindByTypeDictionary.TryGetValue(findByType, out layerName))
			{
				base.coreOptions.layerName = layerName;
			}

			var propertyName = "";
			if (propertyNameFromFindByTypeDictionary.TryGetValue(findByType, out propertyName))
			{
				//TODO: assing filter options by cateory or name
			}
		}

		#region Public Propeerties

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Mapbox.Unity.Map.PrefabItemOptions"/> item is active.
		/// </summary>
		/// <value><c>true</c> if is active; otherwise, <c>false</c>.</value>
		public bool isActive
		{
			get
			{
				return base.coreOptions.isActive;
			}
			set
			{
				base.coreOptions.isActive = value;
			}
		}

		public bool snapToTerrain
		{
			get
			{
				return base.coreOptions.snapToTerrain;
			}
			set
			{
				base.coreOptions.snapToTerrain = value;
			}
		}

		public string prefabItemName
		{
			get
			{
				return base.coreOptions.sublayerName;
			}
			set
			{
				base.coreOptions.sublayerName = value;
			}
		}

		/// <summary>
		/// The prefab to be spawned on the map
		/// </summary>
		public GameObject prefab;

		/// <summary>
		/// The FindbyType enum to specify the type of prefanb item in the list
		/// </summary>
		public LocationPrefabFindBy findByType = LocationPrefabFindBy.MapboxCategory;//default to Mapbox Category

		#endregion


	}
}