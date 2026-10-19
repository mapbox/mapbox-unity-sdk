using System;
using System.ComponentModel;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorTile.Geometry;
using UnityEngine;

namespace Mapbox.VectorModule.Filters
{
    [CreateAssetMenu(menuName = "Mapbox/Filters/Geometry Type Filter")]
    [DisplayName("Geometry Type Filter")]
    public class GeometryTypeFilterObject : FilterBaseObject
    {
        [SerializeField] private GeometryTypeEnum Value;
        [NonSerialized] private GeometryTypeFilter _filter;

        public override ILayerFeatureFilterComparer Filter
        {
            get
            {
                if (_filter == null)
                    _filter = new GeometryTypeFilter(Value);
                return _filter;
            }
        }
    }
    
    [Serializable]
    public class GeometryTypeFilter : FilterBase
    {
        private GeometryTypeEnum _value;

        public GeometryTypeFilter(GeometryTypeEnum value)
        {
            _value = value;
        }

        public override bool Try(VectorFeatureUnity feature)
        {
            // Switched on the enum rather than its lowercased name. The string version matched
            // GeoJSON-style names, of which half could never arrive: an MVT GeomType is only
            // UNKNOWN, POINT, LINESTRING or POLYGON, so the "multipolygon", "multilinestring" and
            // "multipoint" arms were unreachable - and the first of those had a space inside the
            // literal ("case\" multipolygon\":"), so it could not have matched even if MVT produced
            // one. Multi-part geometry arrives as several rings on a single feature, not as a
            // distinct type. Also drops a ToString plus ToLowerInvariant per feature.
            switch (feature.Data.GeometryType)
            {
                case GeomType.POLYGON:
                    return _value == GeometryTypeEnum.Polygon;
                case GeomType.LINESTRING:
                    return _value == GeometryTypeEnum.Line;
                case GeomType.POINT:
                    return _value == GeometryTypeEnum.Point;
                default:
                    return false;
            }
        }
    }

    public enum GeometryTypeEnum
    {
        Polygon,
        Line,
        Point
    }


    
}