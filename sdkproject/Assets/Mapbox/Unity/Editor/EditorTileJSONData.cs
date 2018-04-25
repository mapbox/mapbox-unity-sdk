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

	public bool tileJSONLoaded = false;
	/// <summary>
	/// List of data sources (tileset ids) linked to a layer name
	/// </summary>
	public Dictionary<string, List<string>> LayerPropertyDictionary = new Dictionary<string, List<string>>();

	/// <summary>
	/// The description of the property in a layer
	/// </summary>
	public Dictionary<string, string> PropertyDescriptionDictionary = new Dictionary<string, string>();

	/// <summary>
	/// List of data sources (tileset ids) linked to a layer name
	/// </summary>
	public Dictionary<string, List<string>> LayerSourcesDictionary = new Dictionary<string, List<string>>();

	/// <summary>
	/// The source layers in a dictionary
	/// </summary>
	public Dictionary<string, List<string>> SourceLayersDictionary = new Dictionary<string, List<string>>();

	/// <summary>
	/// The data type of the property name in the layer
	/// </summary>
	public Dictionary<string, PropertyDataType> PropertyDataTypeDictionary = new Dictionary<string, PropertyDataType>();
}
