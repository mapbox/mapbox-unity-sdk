using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.BaseModule.Unity
{
    [Serializable]
    public class UnityTileTerrainContainer
    {
        public TileContainerState State = TileContainerState.Final;
        public Action<UnityMapTile> ElevationValuesUpdated = (t) => { };
        
        private string _elevationMultiplierFieldNameID = "_ElevationMultiplier";
        private string _elevationChangeTimerFieldNameID = "_ElevationChangeTime";
        private string _shaderElevationTextureScaleOffsetFieldNameID = "_HeightTexture_ST";
        private string _shaderElevationTextureFieldNameID = "_HeightTexture";
		
        private UnityMapTile _unityMapTile;
        [SerializeField] public TerrainData TerrainData;
        private Vector4 _terrainTextureScaleOffset;

        public UnityTileTerrainContainer(UnityMapTile unityMapTile)
        {
            _unityMapTile = unityMapTile;
        }

        public void SetTerrainData(TerrainData terrainData, bool useShaderElevation, TileContainerState state = TileContainerState.Final)
        {
            State = state;
            if (terrainData.Texture == null || terrainData.TileId.Z == 0)
            {
                Debug.Log("no texture?");
            }
            TerrainData = terrainData;
            OnTerrainUpdated();
            if (TerrainData.IsElevationDataReady)
            {
                OnElevationValuesUpdated();
            }

            TerrainData.ElevationValuesUpdated += OnElevationValuesUpdated;

            _unityMapTile.Material.SetFloat(_elevationMultiplierFieldNameID, useShaderElevation ? 1 : 0);
            FixMeshBounds(useShaderElevation);
        }

        private void FixMeshBounds(bool useShaderElevation)
        {
            Mesh mesh = _unityMapTile.MeshFilter.mesh;
            if (mesh == null)
            {
                return;
            }

            float elevation = useShaderElevation ? GetMaxExtent() : 0f;
            Vector3 newExtents = mesh.bounds.extents;
            newExtents.y = elevation;
            mesh.bounds = new Bounds(mesh.bounds.center, newExtents);
        }

        private float GetMaxExtent()
        {
            if (TerrainData == null || TerrainData.ElevationValues == null)
            {
                return 0;
            }

            float max = 0;
            float min = float.MaxValue;
            for (int i = 0; i < TerrainData.ElevationValues.Length; i++)
            {
                if (TerrainData.ElevationValues[i] > max)
                {
                    max = TerrainData.ElevationValues[i];
                }
                if (TerrainData.ElevationValues[i] < min)
                {
                    min = TerrainData.ElevationValues[i];
                }
            }
            return Mathf.Max(Mathf.Abs(min), max);
        }

        public void OnTerrainUpdated()
        {
            if (TerrainData == null)
                return;
        
            _terrainTextureScaleOffset = _unityMapTile.CanonicalTileId.CalculateScaleOffsetAtZoom(TerrainData.TileId.Z);
            
            _unityMapTile.Material.SetVector(_shaderElevationTextureScaleOffsetFieldNameID, _terrainTextureScaleOffset);
            _unityMapTile.Material.SetTexture(_shaderElevationTextureFieldNameID, TerrainData.Texture);

            //_unityMapTile._material.SetFloat(_tileScaleFieldNameID, _unityMapTile.TileScale);
            _unityMapTile.Material.SetFloat("_IsFallbackTexture", 0);
            _unityMapTile.Material.SetFloat(_elevationChangeTimerFieldNameID, Time.time);
        }

        public void OnElevationValuesUpdated()
        {
            if (TerrainData == null)
            {
                Debug.Log("TerrainData is null, missing a isRecycled check?");
                return;
            }
            TerrainData.IsElevationDataReady = true;
            ElevationValuesUpdated(_unityMapTile);
        }

        public TerrainData GetAndClearTerrainData()
        {
            if (TerrainData == null)
                return null;

            TerrainData.ElevationValuesUpdated -= OnElevationValuesUpdated;
            _unityMapTile.Material.SetTexture(_shaderElevationTextureFieldNameID, Texture2D.grayTexture);
            var rd = TerrainData;
            TerrainData = null;
            return rd;
        }
        
        public float QueryHeightData(float x, float y)
        {
            if (TerrainData != null && TerrainData.ElevationValues.Length > 0)
            {
                var width = (int)Mathf.Sqrt(TerrainData.ElevationValues.Length);
                var sectionWidth = width * _terrainTextureScaleOffset.x - 1;
                var padding = width * new Vector2(_terrainTextureScaleOffset.z, _terrainTextureScaleOffset.w);
                
                var xx = padding.x + (x * sectionWidth);
                var yy = padding.y + (y * sectionWidth);

                var index = (int) yy * width
                            + (int) xx;
                if (TerrainData.ElevationValues.Length <= index)
                {
                    return 0;
                }
                else
                {
                    return TerrainData.ElevationValues[(int) yy * width + (int) xx];
                }

            }
            return 0;
        }

        public void DisableTerrain()
        {
            State = TileContainerState.Final;
            _unityMapTile.Material.SetFloat(_elevationMultiplierFieldNameID, 0);
        }

        public void OnDestroy()
        {
            if (TerrainData != null)
            {
                TerrainData.ElevationValuesUpdated -= OnElevationValuesUpdated;
                TerrainData = null;
            }
        }
    }
    
    public enum TileContainerState
    {
        Temporary,
        Final
    }
}
