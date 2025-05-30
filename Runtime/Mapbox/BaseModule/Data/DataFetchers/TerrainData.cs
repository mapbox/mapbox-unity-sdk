using System;
using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    [Serializable]
    public class TerrainData : RasterData
    {
        [HideInInspector] public float[] ElevationValues;
        public bool IsElevationDataReady = false;
        public Action ElevationValuesUpdated = () => { };
        
        public override void Clear()
        {
            base.Clear();
            IsElevationDataReady = false;
        }

        public void SetElevationValues(float[] elevationArray)
        {
            ElevationValues = elevationArray;
            IsElevationDataReady = true;
            ElevationValuesUpdated();
        }
        
        public float QueryHeightData(CanonicalTileId requestingSubTileId, float x, float y)
        {
            if (ElevationValues?.Length > 0)
            {
                var _terrainTextureScaleOffset = requestingSubTileId.CalculateScaleOffsetAtZoom(TileId.Z);
                return ReadElevation(x, y, _terrainTextureScaleOffset);
            }
            return 0;
        }
        
        public float QueryHeightData(Vector2 point)
        {
            return ReadElevation(point.x, point.y, new Vector4(1, 1, 0, 0));
        }
        
        public float QueryHeightData(float x, float y)
        {
            return ReadElevation(x, y, new Vector4(1, 1, 0, 0));
        }

        private float ReadElevation(float x, float y, Vector4 terrainTextureScaleOffset)
        {
            var width = (int) Mathf.Sqrt(ElevationValues.Length);
            var sectionWidth = width * terrainTextureScaleOffset.x - 1;
            var padding = width * new Vector2(terrainTextureScaleOffset.z, terrainTextureScaleOffset.w);

            var xx = padding.x + (x * sectionWidth);
            var yy = padding.y + (y * sectionWidth);

            var index = (int) yy * width
                        + (int) xx;
            if (ElevationValues.Length <= index)
            {
                return 0;
            }
            else
            {
                // Bilinear interpolation
                int x1 = (int) xx;
                int y1 = (int) yy;
                int x2 = x1 + 1;
                int y2 = y1 + 1;
                if (x2 >= width)
                {
                    x2 = x1;
                }
                if (y2 >= width)
                {
                    y2 = y1;
                }
                float x2y1 = ElevationValues[y1 * width + x2];
                float x1y2 = ElevationValues[y2 * width + x1];
                float x2y2 = ElevationValues[y2 * width + x2];
                float x1y1 = ElevationValues[y1 * width + x1];
                float x1y = Mathf.Lerp(x1y1, x2y1, xx - x1);
                float x2y = Mathf.Lerp(x1y2, x2y2, xx - x1);
                return Mathf.Lerp(x1y, x2y, yy - y1);
            }
        }

        
    }
}
