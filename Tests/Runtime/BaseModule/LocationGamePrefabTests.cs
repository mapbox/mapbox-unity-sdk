using System;
using System.Collections;
using Mapbox.BaseModule.Map;
using Mapbox.Example.Scripts.Map;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mapbox.BaseModuleTests.PlayModeTests
{
    [TestFixture]
    internal class LocationGamePrefabTests
    {
        private GameObject _locationMap;
        private MapboxMapBehaviour _mapCore;
        MapboxMap _map = null;
        private bool _firstViewLoaded;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _locationMap = Resources.Load("LocationMapPrefab") as GameObject;
        }

        [Test, Order(1)]
        public void CreateLocationMapPrefab()
        {
            GameObject.Instantiate(_locationMap);
        }
    
        [UnityTest, Order(2)]
        public IEnumerator FindAndRegisterToMapNoInitialize()
        {
            
            _firstViewLoaded = false;
            _mapCore = GameObject.FindObjectOfType<MapboxMapBehaviour>();
            void OnMapCoreInitialized(MapboxMap mapboxMap)
            {
                _map = mapboxMap;
                _map.LoadViewCompleted += () => _firstViewLoaded = true;
                Assert.IsNotNull(mapboxMap);
            }

            Assert.IsFalse(_mapCore.InitializeOnStart);
            _mapCore.Initialized += OnMapCoreInitialized;

            yield return new WaitForSeconds(2);
        
            Assert.IsNull(_map);
            Assert.IsFalse(_firstViewLoaded); 
        
            _mapCore.Initialized -= OnMapCoreInitialized;
        }
    
        [UnityTest, Order(3)]
        public IEnumerator FindAndRegisterToMapInitialize()
        {
            void OnMapCoreInitialized(MapboxMap mapboxMap)
            {
                if (mapboxMap == null) throw new ArgumentNullException(nameof(mapboxMap));
                _map = mapboxMap;
                _map.LoadViewCompleted += () => _firstViewLoaded = true;
                Assert.IsNotNull(mapboxMap);
            }

            _mapCore.Initialized += OnMapCoreInitialized;

            _mapCore.Initialize();
        
            while(_mapCore.InitializationStatus < InitializationStatus.ReadyForUpdates) yield return null;
        
            Assert.IsNotNull(_map);
            Assert.IsTrue(_map.Status > InitializationStatus.Initialized);
            Assert.IsNotNull(_map.MapVisualizer);
        
            _mapCore.Initialized -= OnMapCoreInitialized;
        }

        [Test, Order(4)]
        public void DestroyLocationMapPrefab()
        {
            GameObject.DestroyImmediate(_mapCore.gameObject);
            if(_mapCore)
                Assert.Fail();
            else
                Assert.Pass();
        
        }
    }
}