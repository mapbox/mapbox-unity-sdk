using System;
using System.Collections.Generic;


namespace Mapbox.VectorTile.Contants
{


	/// <summary>
	/// PBF wire types
	/// </summary>
	public enum WireTypes
	{
		VARINT = 0,// varint: int32, int64, uint32, uint64, sint32, sint64, bool, enum
		FIXED64 = 1, // 64-bit: double, fixed64, sfixed64
		BYTES = 2, // length-delimited: string, bytes, embedded messages, packed repeated fields
		FIXED32 = 5, // 32-bit: float, fixed32, sfixed32
		UNDEFINED = 99
	}


	/// <summary>
	/// Vector tile geometry commands https://github.com/mapbox/vector-tile-spec/tree/master/2.1#431-command-integers
	/// </summary>
	public enum Commands
	{
		MoveTo = 1,
		LineTo = 2,
		ClosePath = 7
	}


	/// <summary>
	/// Root types contained in the vector tile. Currently just 'Layers' https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L75
	/// </summary>
	public enum TileType
	{
		Layers = 3
	}


	/// <summary>
	/// Types contained in a layer https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L50-L73
	/// </summary>
	public enum LayerType
	{
		Version = 15,
		Name = 1,
		Features = 2,
		Keys = 3,
		Values = 4,
		Extent = 5
	}


	/// <summary>
	/// Types contained in a feature https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L31-L47
	/// </summary>
	public enum FeatureType
	{
		Id = 1,
		Tags = 2,
		Type = 3,
		Geometry = 4,
		Raster = 5
	}


	/// <summary>
	/// Available ypes for values https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L17-L28
	/// </summary>
	public enum ValueType
	{
		String = 1,
		Float = 2,
		Double = 3,
		Int = 4,
		UInt = 5,
		SInt = 6,
		Bool = 7
	}


	/// <summary>
	/// [wip] Investigate how to increase decoding speed. Looking up values in enums is slow
	/// </summary>
	public static class ConstantsAsDictionary
	{


		/// <summary>
		/// Root types contained in the vector tile. Currently just 'Layers' https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L75
		/// </summary>
		public static readonly Dictionary<int, string> TileType = new Dictionary<int, string>()
		{
			{3, "Layers" }
		};


		/// <summary>
		/// Types contained in a layer https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L50-L73
		/// </summary>
		public static readonly Dictionary<int, string> LayerType = new Dictionary<int, string>()
		{
			{15, "Version" },
			{1, "Name" },
			{2,"Features" },
			{3, "Keys" },
			{4, "Values" },
			{5, "Extent" }
		};

		/// <summary>
		/// Types contained in a feature https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L31-L47
		/// </summary>
		public static readonly Dictionary<int, string> FeatureType = new Dictionary<int, string>()
		{
			{ 1, "Id"},
			{ 2, "Tags"},
			{ 3, "Type"},
			{ 4, "Geometry"},
			{ 5, "Raster"}
		};


		/// <summary>
		/// Available geometry types https://github.com/mapbox/vector-tile-spec/tree/master/2.1#434-geometry-types
		/// </summary>
		public static readonly Dictionary<int, string> GeomType = new Dictionary<int, string>()
		{
			{ 0, "Unknown"},
			{ 1, "Point"},
			{ 2, "LineString"},
			{ 3, "Polygon"}
		};
	}

}