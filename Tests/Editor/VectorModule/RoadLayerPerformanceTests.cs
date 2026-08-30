using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer;
using Mapbox.VectorModule.MeshGeneration;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.VectorModuleTests
{
    public class RoadLayerPerformanceTests
    {
        private int SampleCount = 100;
        private ResilientWebRequestFileSource _fs;
        private byte[] buffer;
        private CanonicalTileId tileId = new CanonicalTileId(15, 18654, 9481);
        private string LatLng = "60.1664427,24.9318587";
        private string _roadVisualizerAssetPath = "Packages/com.mapbox.sdk/Tests/Editor/VectorModule/RoadSetup/TEST_BasicRoads.asset";
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var mapboxContext = new MapboxContext();
            _fs = new ResilientWebRequestFileSource(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
            
            Response response = null;
            var req = _fs.Request("https://api.mapbox.com/v4/mapbox.mapbox-streets-v7/" + tileId + ".vector.pbf", (r) =>
            {
                response = r;
            });
            yield return TestWait.While(() => !req.IsCompleted, "SetUp: !req.IsCompleted");
            
            buffer = response.Data;
            Assert.NotZero(buffer.Length);
        }
        
        private IMapInformation GetMapInformation()
        {
            var mapInformation = new MapInformation(LatLng);
            mapInformation.SetInformation(null, 16, 0, 0, 1000);
            mapInformation.Initialize();
            return mapInformation;
        }
        
        [UnityTest, Performance]
        public IEnumerator RoadVisualizer()
        {
            //var viz = new RoadComponentVisualizer("test", GetMapInformation(), null, new RoadComponentSettings());
            var vizObj = (RoadComponentVisualizerObject) AssetDatabase.LoadAssetAtPath(_roadVisualizerAssetPath, typeof(RoadComponentVisualizerObject));
            var viz = (RoadComponentVisualizer) vizObj.ConstructLayerVisualizer(GetMapInformation(), null);
            yield return viz.Initialize();
            
            Measure.Method(() =>
                {
                    var decompressed = Compression.Decompress(buffer);
                    var tt = new VectorModule.ComponentSystem.Data.VectorTile(decompressed);
                    viz.ClearCaches();
                    if (tt.TryGetLayer("road", out var layer))
                    {
                        viz.CreateMesh(tileId, layer);
                    }
                })
                .WarmupCount(5)
                .MeasurementCount(SampleCount)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }
        
        [UnityTest, Performance]
        public IEnumerator SingleRoadVisualizer()
        {
            //var viz = new RoadComponentVisualizer("test", GetMapInformation(), null, new RoadComponentSettings());
            var vizObj = (RoadComponentVisualizerObject) AssetDatabase.LoadAssetAtPath(_roadVisualizerAssetPath, typeof(RoadComponentVisualizerObject));
            var viz = (RoadComponentVisualizer) vizObj.ConstructLayerVisualizer(GetMapInformation(), null);
            yield return viz.Initialize();
            
            Measure.Method(() =>
                {
                    var decompressed = Compression.Decompress(buffer);
                    var tt = new VectorModule.ComponentSystem.Data.VectorTile(decompressed);
                    viz.ClearCaches();
                    if (tt.TryGetLayer("road", out var layer))
                    {
                        viz.CreateMesh(tileId, layer);
                    }
                })
                .WarmupCount(0)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }
        
        [Test, Performance]
        public void RoadVisualizerOld()
        {
            var reg = RegularRoadLayer(GetMapInformation());
            Measure.Method(() =>
                {
                    var decompressed = Compression.Decompress(buffer);
                    var tt = new VectorTile.VectorTile(decompressed, false);
                    reg.ClearCaches();
                    var layer = tt.GetLayer("road");
                    //if (tt.TryGetLayer("building", out var layer))
                    {
                        reg.CreateMesh(tileId, layer);
                    }
                })
                .WarmupCount(5)
                .MeasurementCount(SampleCount)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }
        
        private VectorLayerVisualizer RegularRoadLayer(IMapInformation mapInformation)
        {
            var viz = new VectorLayerVisualizer("test", mapInformation, null, null);
            var modStackSettings = new ModifierStackSettings() { MergeObjects = true };
            var modStack = new ModifierStack(modStackSettings, null);
            modStack.MeshModifiers.Add(new LineMeshForPolygonsModifier(new LineMeshParameters()
            {
                CapType = JoinType.Butt,
                JoinType = JoinType.Round,
                Width = 6
            } ));

            viz.AddModifierStack(new List<ModifierStack>() { modStack });
            viz.Initialize();
            return viz;
        }

    }
}