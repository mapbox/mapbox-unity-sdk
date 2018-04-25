using System.Collections.Generic;
using Mapbox.Unity.Map;

public class EditorTileJSONData
{

	private static EditorTileJSONData _instance;
	public static EditorTileJSONData Instance{
		get
		{
			if (_instance == null)
				_instance = new EditorTileJSONData();

			return _instance;
		}
	}
	public readonly string commonLayersKey = "Common layers across all sources";

	/// <summary>
	/// This boolean is to check if tile JSON data has loaded after the data source has changed
	/// </summary>
	public bool tileJSONLoaded = false;

	/// <summary>
	/// The description of the property in a layer
	/// </summary>
	public Dictionary<string, Dictionary<string,string>> LayerPropertyDescriptionDictionary = new Dictionary<string, Dictionary<string, string>>();

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
		LayerPropertyDescriptionDictionary = new Dictionary<string, Dictionary<string, string>>();
		LayerSourcesDictionary = new Dictionary<string, List<string>>();
		SourceLayersDictionary = new Dictionary<string, List<string>>();
	}
}
