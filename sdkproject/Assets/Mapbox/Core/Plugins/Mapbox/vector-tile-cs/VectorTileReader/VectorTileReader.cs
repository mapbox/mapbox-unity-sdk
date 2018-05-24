using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Mapbox.VectorTile.Contants;
using Mapbox.VectorTile.Geometry;

#if !NET20
using System.Linq;
#endif

namespace Mapbox.VectorTile
{




	/// <summary>
	/// Mail vector tile reader class
	/// </summary>
	public class VectorTileReader
	{

		/// <summary>
		/// Initialize VectorTileReader
		/// </summary>
		/// <param name="data">Byte array containing the raw (already unzipped) tile data</param>
		/// <param name="validate">If true, run checks if the tile contains valid data. Decreases decoding speed.</param>
		public VectorTileReader(byte[] data, bool validate = true)
		{
			if (null == data)
			{
				throw new System.Exception("Tile data cannot be null");
			}
			if (data.Length < 1)
			{
				throw new System.Exception("Tile data cannot be empty");
			}
			if (data[0] == 0x1f && data[1] == 0x8b)
			{
				throw new System.Exception("Tile data is zipped");
			}

			_Validate = validate;
			layers(data);
		}


		private Dictionary<string, byte[]> _Layers = new Dictionary<string, byte[]>();
		private bool _Validate;


		private void layers(byte[] data)
		{
			PbfReader tileReader = new PbfReader(data);
			while (tileReader.NextByte())
			{
				if (_Validate)
				{
					if (!ConstantsAsDictionary.TileType.ContainsKey(tileReader.Tag))
					{
						throw new System.Exception(string.Format("Unknown tile tag: {0}", tileReader.Tag));
					}
				}
				if (tileReader.Tag == (int)TileType.Layers)
				{
					string name = null;
					byte[] layerMessage = tileReader.View();
					PbfReader layerView = new PbfReader(layerMessage);
					while (layerView.NextByte())
					{
						if (layerView.Tag == (int)LayerType.Name)
						{
							ulong strLen = (ulong)layerView.Varint();
							name = layerView.GetString(strLen);
						}
						else
						{
							layerView.Skip();
						}
					}
					if (_Validate)
					{
						if (string.IsNullOrEmpty(name))
						{
							throw new System.Exception("Layer missing name");
						}
						if (_Layers.ContainsKey(name))
						{
							throw new System.Exception(string.Format("Duplicate layer names: {0}", name));
						}
					}
					_Layers.Add(name, layerMessage);
				}
				else
				{
					tileReader.Skip();
				}
			}
		}


		/// <summary>
		/// Collection of layers contained in the tile
		/// </summary>
		/// <returns>Collection of layer names</returns>
		public ReadOnlyCollection<string> LayerNames()
		{
#if NET20 || PORTABLE || WINDOWS_UWP
			string[] lyrNames = new string[_Layers.Keys.Count];
			_Layers.Keys.CopyTo(lyrNames, 0);
			return new ReadOnlyCollection<string>(lyrNames);
#else
			return _Layers.Keys.ToList().AsReadOnly();
#endif
		}

		/// <summary>
		/// Get a tile layer by name
		/// </summary>
		/// <param name="layerName">Name of the layer to request</param>
		/// <returns>Decoded <see cref="VectorTileLayer"/></returns>
		public VectorTileLayer GetLayer(string name)
		{
			if (!_Layers.ContainsKey(name))
			{
				return null;
			}

			return getLayer(_Layers[name]);
		}


		private VectorTileLayer getLayer(byte[] data)
		{
			VectorTileLayer layer = new VectorTileLayer(data);
			PbfReader layerReader = new PbfReader(layer.Data);
			while (layerReader.NextByte())
			{
				int layerType = layerReader.Tag;
				if (_Validate)
				{
					if (!ConstantsAsDictionary.LayerType.ContainsKey(layerType))
					{
						throw new System.Exception(string.Format("Unknown layer type: {0}", layerType));
					}
				}
				switch ((LayerType)layerType)
				{
					case LayerType.Version:
						ulong version = (ulong)layerReader.Varint();
						layer.Version = version;
						break;
					case LayerType.Name:
						ulong strLength = (ulong)layerReader.Varint();
						layer.Name = layerReader.GetString(strLength);
						break;
					case LayerType.Extent:
						layer.Extent = (ulong)layerReader.Varint();
						break;
					case LayerType.Keys:
						byte[] keyBuffer = layerReader.View();
						string key = Encoding.UTF8.GetString(keyBuffer, 0, keyBuffer.Length);
						layer.Keys.Add(key);
						break;
					case LayerType.Values:
						byte[] valueBuffer = layerReader.View();
						PbfReader valReader = new PbfReader(valueBuffer);
						while (valReader.NextByte())
						{
							switch ((ValueType)valReader.Tag)
							{
								case ValueType.String:
									byte[] stringBuffer = valReader.View();
									string value = Encoding.UTF8.GetString(stringBuffer, 0, stringBuffer.Length);
									layer.Values.Add(value);
									break;
								case ValueType.Float:
									float snglVal = valReader.GetFloat();
									layer.Values.Add(snglVal);
									break;
								case ValueType.Double:
									double dblVal = valReader.GetDouble();
									layer.Values.Add(dblVal);
									break;
								case ValueType.Int:
									long i64 = valReader.Varint();
									layer.Values.Add(i64);
									break;
								case ValueType.UInt:
									long u64 = valReader.Varint();
									layer.Values.Add(u64);
									break;
								case ValueType.SInt:
									long s64 = valReader.Varint();
									layer.Values.Add(s64);
									break;
								case ValueType.Bool:
									long b = valReader.Varint();
									layer.Values.Add(b == 1);
									break;
								default:
									throw new System.Exception(string.Format(
										NumberFormatInfo.InvariantInfo
										, "NOT IMPLEMENTED valueReader.Tag:{0} valueReader.WireType:{1}"
										, valReader.Tag
										, valReader.WireType
									));
									//uncomment the following lines when not throwing!!
									//valReader.Skip();
									//break;
							}
						}
						break;
					case LayerType.Features:
						layer.AddFeatureData(layerReader.View());
						break;
					default:
						layerReader.Skip();
						break;
				}
			}

			if (_Validate)
			{
				if (string.IsNullOrEmpty(layer.Name))
				{
					throw new System.Exception("Layer has no name");
				}
				if (0 == layer.Version)
				{
					throw new System.Exception(string.Format("Layer [{0}] has invalid version. Only version 2.x of 'Mapbox Vector Tile Specification' (https://github.com/mapbox/vector-tile-spec) is supported.", layer.Name));
				}
				if (2 != layer.Version)
				{
					throw new System.Exception(string.Format("Layer [{0}] has invalid version: {1}. Only version 2.x of 'Mapbox Vector Tile Specification' (https://github.com/mapbox/vector-tile-spec) is supported.", layer.Name, layer.Version));
				}
				if (0 == layer.Extent)
				{
					throw new System.Exception(string.Format("Layer [{0}] has no extent.", layer.Name));
				}
				if (0 == layer.FeatureCount())
				{
					throw new System.Exception(string.Format("Layer [{0}] has no features.", layer.Name));
				}
				//TODO: find equivalent of 'Distinct()' for NET20
#if !NET20
				if (layer.Values.Count != layer.Values.Distinct().Count()) {
					throw new System.Exception(string.Format("Layer [{0}]: duplicate attribute values found", layer.Name));
				}
#endif
			}

			return layer;
		}


		/// <summary>
		/// Get a feature of the <see cref="VectorTileLayer"/>
		/// </summary>
		/// <param name="layer"><see cref="VectorTileLayer"/> containing the feature</param>
		/// <param name="data">Raw byte data of the feature</param>
		/// <param name="validate">If true, run checks if the tile contains valid data. Decreases decoding speed.</param>
		/// <param name="clippBuffer">
		/// <para>'null': returns the geometries unaltered as they are in the vector tile. </para>
		/// <para>Any value >=0 clips a border with the size around the tile. </para>
		/// <para>These are not pixels but the same units as the 'extent' of the layer. </para>
		/// </param>
		/// <returns></returns>
		public static VectorTileFeature GetFeature(
			VectorTileLayer layer
			, byte[] data
			, bool validate = true
			, uint? clipBuffer = null
			, float scale = 1.0f
		)
		{

			PbfReader featureReader = new PbfReader(data);
			VectorTileFeature feat = new VectorTileFeature(layer, clipBuffer, scale);
			bool geomTypeSet = false;
			while (featureReader.NextByte())
			{
				int featureType = featureReader.Tag;
				if (validate)
				{
					if (!ConstantsAsDictionary.FeatureType.ContainsKey(featureType))
					{
						throw new System.Exception(string.Format("Layer [{0}] has unknown feature type: {1}", layer.Name, featureType));
					}
				}
				switch ((FeatureType)featureType)
				{
					case FeatureType.Id:
						feat.Id = (ulong)featureReader.Varint();
						break;
					case FeatureType.Tags:
#if NET20
						List<int> tags = featureReader.GetPackedUnit32().ConvertAll<int>(ui => (int)ui);
#else
						List<int> tags = featureReader.GetPackedUnit32().Select(t => (int)t).ToList();
#endif
						feat.Tags = tags;
						break;
					case FeatureType.Type:
						int geomType = (int)featureReader.Varint();
						if (validate)
						{
							if (!ConstantsAsDictionary.GeomType.ContainsKey(geomType))
							{
								throw new System.Exception(string.Format("Layer [{0}] has unknown geometry type tag: {1}", layer.Name, geomType));
							}
						}
						feat.GeometryType = (GeomType)geomType;
						geomTypeSet = true;
						break;
					case FeatureType.Geometry:
						if (null != feat.GeometryCommands)
						{
							throw new System.Exception(string.Format("Layer [{0}], feature already has a geometry", layer.Name));
						}
						//get raw array of commands and coordinates
						feat.GeometryCommands = featureReader.GetPackedUnit32();
						break;
					default:
						featureReader.Skip();
						break;
				}
			}

			if (validate)
			{
				if (!geomTypeSet)
				{
					throw new System.Exception(string.Format("Layer [{0}]: feature missing geometry type", layer.Name));
				}
				if (null == feat.GeometryCommands)
				{
					throw new System.Exception(string.Format("Layer [{0}]: feature has no geometry", layer.Name));
				}
				if (0 != feat.Tags.Count % 2)
				{
					throw new System.Exception(string.Format("Layer [{0}]: uneven number of feature tag ids", layer.Name));
				}
				if (feat.Tags.Count > 0)
				{
#if NET20
					int maxKeyIndex = -9999;
					int tagCount = feat.Tags.Count;
					for (int i = 0; i < tagCount; i += 2)
					{
						if (feat.Tags[i] > maxKeyIndex) { maxKeyIndex = feat.Tags[i]; }
					}
					int maxValueIndex = -9999;
					for (int i = 1; i < tagCount; i += 2)
					{
						if (feat.Tags[i] > maxValueIndex) { maxValueIndex = feat.Tags[i]; }
					}
#else
					int maxKeyIndex = feat.Tags.Where((key, idx) => idx % 2 == 0).Max();
					int maxValueIndex = feat.Tags.Where((key, idx) => (idx + 1) % 2 == 0).Max();
#endif
					if (maxKeyIndex >= layer.Keys.Count)
					{
						throw new System.Exception(string.Format("Layer [{0}]: maximum key index equal or greater number of key elements", layer.Name));
					}
					if (maxValueIndex >= layer.Values.Count)
					{
						throw new System.Exception(string.Format("Layer [{0}]: maximum value index equal or greater number of value elements", layer.Name));
					}
				}
			}

			return feat;
		}



	}
}
