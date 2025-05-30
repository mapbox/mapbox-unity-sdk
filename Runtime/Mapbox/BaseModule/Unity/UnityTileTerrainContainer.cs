using System;
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

        public void FixMeshBounds(bool useShaderElevation)
        {
            Mesh mesh = _unityMapTile.MeshFilter.mesh;
            if (mesh != null && mesh.vertexCount != 0)
            {
                mesh.RecalculateBounds();
                if (useShaderElevation)
                {
                    mesh.bounds = GetBoundsAdjustedForElevation();
                }
            }
        }

        private Bounds GetBoundsAdjustedForElevation()
        {
            Mesh mesh = _unityMapTile.MeshFilter.mesh;
            if (TerrainData == null || TerrainData.ElevationValues == null)
            {
                return mesh.bounds;
            }

            float maxY = float.MinValue;
            float minY = float.MaxValue;
            foreach (float t in TerrainData.ElevationValues)
            {
                float elevationScaled = t * _unityMapTile.TileScale;
                maxY = Mathf.Max(maxY, elevationScaled);
                minY = Mathf.Min(minY, elevationScaled);
            }
            Vector3 center = mesh.bounds.center;
            center.y = (maxY + minY) / 2;
            Vector3 size = mesh.bounds.size;
            size.y = maxY - minY;
            return new Bounds(center, size);
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
                return TerrainData.QueryHeightData(_unityMapTile.CanonicalTileId, x, y);
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
