namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Platform.TilesetTileJSON;
	using UnityEngine;

	[Serializable]
	public class TileJsonData
	{
		public readonly string commonLayersKey = "(layer found in more than one data source)";
		public readonly string optionalPropertiesString = "(may not appear across all locations)";
		/// <summary>
		/// This boolean is to check if tile JSON data has loaded after the data source has changed
		/// </summary>
		public bool tileJSONLoaded = false;

		/// <summary>
		/// Layer Display Names seen in the editor
		/// </summary>
		public List<string> LayerDisplayNames = new List<string>();

		/// <summary>
		/// Property Display Names seen in the editor
		/// </summary>
		public Dictionary<string, List<string>> PropertyDisplayNames = new Dictionary<string, List<string>>();

		/// <summary>
		/// The description of the property in a layer
		/// </summary>
		public Dictionary<string, Dictionary<string, string>> LayerPropertyDescriptionDictionary = new Dictionary<string, Dictionary<string, string>>();

		/// <summary>
		/// List of data sources (tileset ids) linked to a layer name
		/// </summary>
		public Dictionary<string, List<string>> LayerSourcesDictionary = new Dictionary<string, List<string>>();

		/// <summary>
		/// Dictionary containting the list of layers in a source
		/// </summary>
		public Dictionary<string, List<string>> SourceLayersDictionary = new Dictionary<string, List<string>>();

		public void ClearData()
		{
			tileJSONLoaded = false;
			LayerPropertyDescriptionDictionary.Clear();
			LayerSourcesDictionary.Clear();
			SourceLayersDictionary.Clear();
			LayerDisplayNames.Clear();
			PropertyDisplayNames.Clear();
		}

		public void ProcessTileJSONData(TileJSONResponse tjr)
		{
			tileJSONLoaded = true;
			List<string> layerPropertiesList = new List<string>();
			// Un-comment if required. Throws a warning right now. 
			//List<string> sourceLayersList = new List<string>();

			if (tjr == null || tjr.VectorLayers == null || tjr.VectorLayers.Length == 0)
			{
				return;
			}

			ClearData();

			var propertyName = "";
			var propertyDescription = "";
			var layerSource = "";

			foreach (var layer in tjr.VectorLayers)
			{
				//load layer names
				var layerName = layer.Id;
				layerPropertiesList = new List<string>();
				layerSource = layer.Source;
				//if (layer.Fields.Count == 0)
				//continue;

				//loading layer sources
				if (LayerSourcesDictionary.ContainsKey(layerName))
				{
					LayerSourcesDictionary[layerName].Add(layerSource);
				}
				else
				{
					LayerSourcesDictionary.Add(layerName, new List<string>() { layerSource });
				}

				//loading layers to a data source
				if (SourceLayersDictionary.ContainsKey(layerSource))
				{
					List<string> sourceList = new List<string>();
					LayerSourcesDictionary.TryGetValue(layerName, out sourceList);

					if (sourceList.Count > 1 && sourceList.Contains(layerSource)) // the current layerName has more than one source
					{
						if (SourceLayersDictionary.ContainsKey(commonLayersKey))
						{
							SourceLayersDictionary[commonLayersKey].Add(layerName);
						}
						else
						{
							SourceLayersDictionary.Add(commonLayersKey, new List<string>() { layerName });
						}

						if (LayerDisplayNames.Contains(layerName))
						{
							LayerDisplayNames.Remove(layerName);
						}
						LayerDisplayNames.Add(layerName);
						//						LayerDisplayNames.Add(layerName + " " + commonLayersKey);
					}
					else
					{
						SourceLayersDictionary[layerSource].Add(layerName);
						LayerDisplayNames.Add(layerName);
					}
				}
				else
				{
					SourceLayersDictionary.Add(layerSource, new List<string>() { layerName });
					LayerDisplayNames.Add(layerName);
				}


				//Load properties
				foreach (var property in layer.Fields)
				{
					propertyName = property.Key;
					propertyDescription = property.Value;
					layerPropertiesList.Add(propertyName);

					//adding property descriptions
					if (LayerPropertyDescriptionDictionary.ContainsKey(layerName))
					{
						if (!LayerPropertyDescriptionDictionary[layerName].ContainsKey(propertyName))
						{
							LayerPropertyDescriptionDictionary[layerName].Add(propertyName, propertyDescription);
						}
					}
					else
					{
						LayerPropertyDescriptionDictionary.Add(layerName, new Dictionary<string, string>() { { propertyName, propertyDescription } });
					}

					//Loading display names for properties
					if (PropertyDisplayNames.ContainsKey(layerName))
					{
						if (!PropertyDisplayNames[layerName].Contains(propertyName))
						{
							PropertyDisplayNames[layerName].Add(propertyName);

							//logic to add the list of masked properties from all sources that are not #1
							if (LayerSourcesDictionary[layerName].Count > 1 && !string.IsNullOrEmpty(tjr.Source))
							{
								var firstSource = tjr.Source.Split(new string[] { "," }, System.StringSplitOptions.None)[0].Trim();
								if (layerSource != firstSource)
								{
									if (PropertyDisplayNames[layerName].Contains(propertyName))
									{
										PropertyDisplayNames[layerName].Remove(propertyName);
									}

									PropertyDisplayNames[layerName].Add(propertyName);
									//PropertyDisplayNames[layerName].Add(propertyName + " " + optionalPropertiesString);
								}
							}
						}
					}
					else
					{
						PropertyDisplayNames.Add(layerName, new List<string> { propertyName });
					}
				}

				if (PropertyDisplayNames.ContainsKey(layerName) && PropertyDisplayNames[layerName].Count > 1)
				{
					PropertyDisplayNames[layerName].Sort();
				}
			}


			if (LayerDisplayNames.Count > 1)
			{
				LayerDisplayNames.Sort();
			}
		}
	}
}