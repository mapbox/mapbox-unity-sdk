using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.MeshGeneration.GameObjectModifiers;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration
{
    public interface IModifierStack
    {
        MeshData RunMeshModifiers(VectorFeatureUnity feature, MeshData meshData, IMapInformation mapInfo);
        void RunGoModifiers(VectorEntity entity, IMapInformation mapInformation);

        void Finalize(VectorEntity entity);
    }

    [Serializable]
    public class ModifierStackSettings
    {
        [Tooltip("Tiles will load and visuals will be visible if and only if current map is in this zoom range.")]
        public Vector2 VisibilityZoomRange = new Vector2(1, 20);
        public bool MergeObjects = true;
        public LayerTypeEnum LayerType;
    }

    [Serializable]
    public class ModifierStack : IModifierStack
    {
        public VectorFilterStack Filters { get; private set; }
        public List<IMeshModifier> MeshModifiers;
        public List<IGameObjectModifier> GoModifiers;
        private ObjectPool<VectorEntity> _objectPool;
        public ModifierStackSettings Settings;
        private int _defaultPoolSize = 20;
        
        public ModifierStack(ModifierStackSettings settings, VectorFilterStack filters = null)
        {
            Filters = filters;
            Settings = settings;
            MeshModifiers = new List<IMeshModifier>();
            GoModifiers = new List<IGameObjectModifier>();
        }
        
        public void Initialize(Transform layerRootObject = null)
        {
            // Ask the stack to initialize its own filters instead of reaching into its list.
            Filters?.Initialize();

            _objectPool = new ObjectPool<VectorEntity>(() => VectorEntityGenerator(layerRootObject));
            _objectPool.InitializeItems(_defaultPoolSize);
        }

        /// <summary>
        /// Bracket a tile's features. Both calls happen on the worker thread that runs the tile's
        /// modifiers, and <see cref="EndTile"/> runs even if the tile yielded no features, so a
        /// modifier holding per-tile scratch always gets it back.
        /// </summary>
        public void BeginTile(CanonicalTileId tileId)
        {
            var counter = MeshModifiers.Count;
            for (int i = 0; i < counter; i++)
            {
                if (MeshModifiers[i] != null)
                {
                    MeshModifiers[i].BeginTile(tileId);
                }
            }
        }

        public void EndTile(CanonicalTileId tileId)
        {
            var counter = MeshModifiers.Count;
            for (int i = 0; i < counter; i++)
            {
                if (MeshModifiers[i] != null)
                {
                    MeshModifiers[i].EndTile(tileId);
                }
            }
        }

        public MeshData RunMeshModifiers(VectorFeatureUnity feature, MeshData meshData, IMapInformation mapInfo)
        {
            var counter = MeshModifiers.Count;
            for (int i = 0; i < counter; i++)
            {
                if (MeshModifiers[i] != null)
                {
                    MeshModifiers[i].Run(feature, meshData, mapInfo);
                }
            }
            return meshData;
        }

        public void RunGoModifiers(VectorEntity entity, IMapInformation mapInformation)
        {
            var counter = GoModifiers.Count;
            for (int i = 0; i < counter; i++)
            {
                GoModifiers[i].Run(entity, mapInformation);
            }
        }

        public void UnregisterTile(CanonicalTileId tileId)
        {
            foreach (var goModifier in GoModifiers)
            {
                goModifier.Unregister(tileId);
            }

            var counter = MeshModifiers.Count;
            for (int i = 0; i < counter; i++)
            {
                if (MeshModifiers[i] != null)
                {
                    MeshModifiers[i].UnregisterTile(tileId);
                }
            }
        }

        public void Finalize(VectorEntity entity)
        {
            foreach (var goModifier in GoModifiers)
            {
                goModifier.Finalize(entity);
            }
            _objectPool.Put(entity);
        }
        
        public void OnDestroy()
        {
            _objectPool?.Clear();
        }
        
        private VectorEntity VectorEntityGenerator(Transform layerRootObject)
        {
            var go = new GameObject("pool item");
            if(layerRootObject != null) 
                go.transform.SetParent(layerRootObject, false);
            // var mf = go.AddComponent<MeshFilter>();
            // mf.sharedMesh = new Mesh();
            // mf.sharedMesh.name = "feature";
            //var mr = go.AddComponent<MeshRenderer>();
            var tempVectorEntity = new VectorEntity()
            {
                GameObject = go,
                Transform = go.transform,
                //MeshFilter = mf,
                //MeshRenderer = mr,
                //Mesh = mf.sharedMesh
            };
            return tempVectorEntity;
        }

        public VectorEntity CreateEntity(MeshData meshData)
        {
            var tempVectorEntity = _objectPool.GetObject();

            // It is possible that we changed scenes in the middle of map generation.
            // This object can be null as a result of Unity cleaning up game objects in the scene.
            // Let's bail if we don't have our object.
            if (tempVectorEntity.GameObject == null)
            {
                return null;
            }

            if (meshData.Vertices.Count > 0)
            {
                if (tempVectorEntity.MeshFilter == null)
                {
                    tempVectorEntity.MeshFilter = tempVectorEntity.GameObject.AddComponent<MeshFilter>();
                    tempVectorEntity.Mesh = new Mesh();
                    tempVectorEntity.MeshFilter.mesh = tempVectorEntity.Mesh;
                }
                
                tempVectorEntity.Mesh.Clear();
                tempVectorEntity.Mesh.SetMeshValues(meshData);
            }
            
            tempVectorEntity.GameObject.name = "go";
            tempVectorEntity.GameObject.SetActive(false);
            tempVectorEntity.Transform.localPosition = meshData.PositionInTile;

            return tempVectorEntity;
        }
        
        public bool IsZinSupportedRange(int targetZ)
        {
            return Settings.VisibilityZoomRange.x <= targetZ && Settings.VisibilityZoomRange.y >= targetZ;
        }
    }
    
    public enum LayerTypeEnum
    {
        Polygon,
        Line,
        Point
    }
}