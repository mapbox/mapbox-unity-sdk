namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using System.Collections;
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class PrefabItemOptions : VectorSubLayerProperties
	{
		//Fixed primitiveType
		private VectorPrimitiveType primitiveType = VectorPrimitiveType.Point;

		//Group features turned off
		private bool groupFeatures = false;

		//No extrusion
		private ExtrusionType extrusionType = ExtrusionType.None;
		private readonly Dictionary<LocationPrefabFindBy, string> layerNameFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, "poi_label"},
			{LocationPrefabFindBy.POIName, "poi_label"},
		};


		private readonly Dictionary<LocationPrefabFindBy, string> propertyNameFromFindByTypeDictionary = new Dictionary<LocationPrefabFindBy, string>
		{
			{LocationPrefabFindBy.AddressOrLatLon, ""},
			{LocationPrefabFindBy.MapboxCategory, "maki"},
			{LocationPrefabFindBy.POIName, "name"},
		};

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
		/// The prefab to be spawned on the map
		/// </summary>
		public GameObject prefab;

		public LocationPrefabFindBy findByType = LocationPrefabFindBy.MapboxCategory;//default to Mapbox Category

		#endregion


	}
}