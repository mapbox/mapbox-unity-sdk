using System;
using System.Collections.Generic;
using Mapbox.VectorTile;
using System.Globalization;
using Mapbox.VectorTile.Geometry;

#if !NET20
using System.Linq;
#endif

namespace Mapbox.VectorTile.ExtensionMethods
{


	public static class VectorTileExtensions
	{


		/// <summary>
		/// Return the whole vector tile as GeoJSON
		/// </summary>
		/// <param name="tile"></param>
		/// <param name="zoom">Zoom level of the tile</param>
		/// <param name="tileColumn">Column of the tile (OSM tile schema)</param>
		/// <param name="tileRow">Row of the tile (OSM tile schema)</param>
		/// <param name="clipBuffer">
		/// <para>Vector tiles may contain data reaching beyond the actual tile border.</para>
		/// <para>Define if/how this data should be included.</para>
		/// <para>'null': data from the vector tile as is</para>
		/// <para>'&gt;=0': clip distance in internal tile units</para>
		/// </param>
		/// <returns>GeoJSON of the vector tile contents</returns>
		public static string ToGeoJson(
			this VectorTile tile
			, ulong zoom
			, ulong tileColumn
			, ulong tileRow
			, uint? clipBuffer = null
		)
		{

			//to get '.' instead of ',' when using "string.format" with double/float and non-US system number format settings
			//CultureInfo en_US = new CultureInfo("en-US");

			// escaping '{' '}' -> @"{{" "}}"
			//escaping '"' -> @""""
			string templateFeatureCollection = @"{{""type"":""FeatureCollection"",""features"":[{0}]}}";
			string templateFeature = @"{{""type"":""Feature"",""geometry"":{{""type"":""{0}"",""coordinates"":[{1}]}},""properties"":{2}}}";

			List<string> geojsonFeatures = new List<string>();

			foreach (var layerName in tile.LayerNames())
			{
				var layer = tile.GetLayer(layerName);

				for (int i = 0; i < layer.FeatureCount(); i++)
				{
					var feat = layer.GetFeature(i, clipBuffer, 1.0f);

					if (feat.GeometryType == GeomType.UNKNOWN) { continue; }

					//resolve properties
					List<string> keyValue = new List<string>();
					int tagCnt = feat.Tags.Count;
					for (int j = 0; j < tagCnt; j += 2)
					{
						string key = layer.Keys[feat.Tags[j]];
						object val = layer.Values[feat.Tags[j + 1]];
						keyValue.Add(string.Format(NumberFormatInfo.InvariantInfo, @"""{0}"":""{1}""", key, val));
					}

					//build geojson properties object from resolved properties
					string geojsonProps = string.Format(
						NumberFormatInfo.InvariantInfo
						, @"{{""id"":{0},""lyr"":""{1}""{2}{3}}}"
						, feat.Id
						, layer.Name
						, keyValue.Count > 0 ? "," : ""
						, string.Join(",", keyValue.ToArray())
					);

					//work through geometries
					string geojsonCoords = "";
					string geomType = feat.GeometryType.Description();

					//multipart
					List<List<LatLng>> geomWgs84 = feat.GeometryAsWgs84(zoom, tileColumn, tileRow);
					if (geomWgs84.Count > 1)
					{
						switch (feat.GeometryType)
						{
							case GeomType.POINT:
								geomType = "MultiPoint";
#if NET20
								List<LatLng> allPoints = new List<LatLng>();
								foreach (var part in geomWgs84)
								{
									foreach (var pnt in part)
									{
										allPoints.Add(pnt);
									}
								}
								geojsonCoords = string.Join(
									","
									, allPoints.ConvertAll<string>(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
								);
#else
								geojsonCoords = string.Join(
									","
									, geomWgs84
										.SelectMany((List<LatLng> g) => g)
										.Select(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
								);
#endif
								break;
							case GeomType.LINESTRING:
								geomType = "MultiLineString";
								List<string> parts = new List<string>();
								foreach (var part in geomWgs84)
								{
									parts.Add("[" + string.Join(
									","
#if NET20
											, part.ConvertAll<string>(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#else
											, part.Select(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#endif
											) + "]");
								}
								geojsonCoords = string.Join(",", parts.ToArray());
								break;
							case GeomType.POLYGON:
								geomType = "MultiPolygon";
								List<string> partsMP = new List<string>();
								foreach (var part in geomWgs84)
								{
									partsMP.Add("[" + string.Join(
									","
#if NET20
											, part.ConvertAll<string>(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#else
											, part.Select(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#endif
											) + "]");
								}
								geojsonCoords = "[" + string.Join(",", partsMP.ToArray()) + "]";
								break;
							default:
								break;
						}
					}
					else if (geomWgs84.Count == 1)
					{ //singlepart
						switch (feat.GeometryType)
						{
							case GeomType.POINT:
								geojsonCoords = string.Format(NumberFormatInfo.InvariantInfo, "{0},{1}", geomWgs84[0][0].Lng, geomWgs84[0][0].Lat);
								break;
							case GeomType.LINESTRING:
								geojsonCoords = string.Join(
									","
#if NET20
											, geomWgs84[0].ConvertAll<string>(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#else
											, geomWgs84[0].Select(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#endif
										);
								break;
							case GeomType.POLYGON:
								geojsonCoords = "[" + string.Join(
									","
#if NET20
											, geomWgs84[0].ConvertAll<string>(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#else
											, geomWgs84[0].Select(g => string.Format(NumberFormatInfo.InvariantInfo, "[{0},{1}]", g.Lng, g.Lat)).ToArray()
#endif
										) + "]";
								break;
							default:
								break;
						}
					}
					else
					{//no geometry
						continue;
					}

					geojsonFeatures.Add(
						string.Format(
							NumberFormatInfo.InvariantInfo
							, templateFeature
							, geomType
							, geojsonCoords
							, geojsonProps
						)
					);
				}
			}

			string geoJsonFeatColl = string.Format(
				NumberFormatInfo.InvariantInfo
				, templateFeatureCollection
				, string.Join(",", geojsonFeatures.ToArray())
			);

			return geoJsonFeatColl;
		}



	}
}


