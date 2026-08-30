using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.MeshGeneration;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using Mapbox.VectorModule.Unity;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.VectorModuleTests
{
    public class BuildingLayerPerformanceTests
    {
        private int SampleCount = 100;
        
        private ResilientWebRequestFileSource _fs;
        private byte[] buffer;
        private CanonicalTileId tileId = new CanonicalTileId(15, 18654, 9481);
        private string LatLng = "60.1664427,24.9318587";
        private string _buildingVisualizerAssetPath = "Packages/com.mapbox.sdk/Tests/Editor/VectorModule/BuildingSetups/TEST_BuildingLayerVisualizerObject.asset";
        private string _oldBuildVisAssetPath = "Packages/com.mapbox.sdk/Tests/Editor/VectorModule/BuildingSetups/TEST_OldLayerVisualizer.asset";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.LoadConfigurationCoroutine(false);
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
        
        private BuildingComponentVisualizer BuildingLayer(IMapInformation mapInformation)
        {
            var viz = new BuildingComponentVisualizer("test", mapInformation, null, null);
            viz.Initialize();
            return viz;
        }

        private VectorLayerVisualizer RegularLayer(IMapInformation mapInformation, bool doChamfer)
        {
            var viz = new VectorLayerVisualizer("test", mapInformation, null, null);
            var modStackSettings = new ModifierStackSettings() { MergeObjects = true };
            var modStack = new ModifierStack(modStackSettings, null);
            modStack.MeshModifiers.Add(new SnapTerrainModifier());
            modStack.MeshModifiers.Add(new PolygonMeshModifier(0));
            if (doChamfer)
            {
                modStack.MeshModifiers.Add(new ChamferHeightModifier(new ChamferModifierSettings()
                    { FlatTops = true, OffsetInMeters = 1 }));
            }
            else
            {
                modStack.MeshModifiers.Add(new HeightModifier(new GeometryExtrusionOptions()));
            }

            viz.AddModifierStack(new List<ModifierStack>() { modStack });
            viz.Initialize();
            return viz;
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

        [Test, Performance, Order(0)]
        public void Decompress()
        {
            Measure.Method(() =>
                {
                    var decompressed = Compression.Decompress(buffer);
                    var tile = new Mapbox.VectorTile.VectorTile(decompressed);
                })
                .WarmupCount(5)
                .MeasurementCount(SampleCount)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }

        private static bool[] _doChamfer = new bool[] { true, false};
        
        
        [UnityTest, Performance, Order(1)]
        public IEnumerator BuildingVisualizer([ValueSource("_doChamfer")] bool doChamfer)
        {
            var settings = doChamfer
                ? new BuildingComponentSettings() { RoundBuildingCorners = true }
                : new BuildingComponentSettings() { RoundBuildingCorners = false };
            var vizObj = (BuildingComponentVisualizerObject) AssetDatabase.LoadAssetAtPath(_buildingVisualizerAssetPath, typeof(BuildingComponentVisualizerObject));
            vizObj.Settings = settings;
            var viz = (BuildingComponentVisualizer) vizObj.ConstructLayerVisualizer(GetMapInformation(), null);
            yield return viz.Initialize();
            
            Measure.Method(() => { RunBuildingComponent(viz); })
                .WarmupCount(5)
                .MeasurementCount(SampleCount)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }
        
        [UnityTest, Performance, Order(3)]
        public IEnumerator SingleBuildingVisualizer([ValueSource("_doChamfer")] bool doChamfer)
        {
            var settings = doChamfer
                ? new BuildingComponentSettings() { RoundBuildingCorners = true }
                : new BuildingComponentSettings() { RoundBuildingCorners = false };
            var vizObj = (BuildingComponentVisualizerObject) AssetDatabase.LoadAssetAtPath(_buildingVisualizerAssetPath, typeof(BuildingComponentVisualizerObject));
            vizObj.Settings = settings;
            var viz = (BuildingComponentVisualizer) vizObj.ConstructLayerVisualizer(GetMapInformation(), null);
            yield return viz.Initialize();
            
            Measure.Method(() => { RunBuildingComponent(viz); })
                .WarmupCount(0)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }
        
        private void RunBuildingComponent(BuildingComponentVisualizer viz)
        {
            var decompressed = Compression.Decompress(buffer);
            var tt = new VectorModule.ComponentSystem.Data.VectorTile(decompressed);
            viz.ClearCaches();
            if (tt.TryGetLayer("building", out var layer))
            {
                viz.CreateMesh(tileId, layer);
            }
        }

        
        [Test, Performance, Order(2)]
        public void BuildingVisualizerOldChamfer()
        {
            var vizObj = (VectorLayerVisualizerObject) AssetDatabase.LoadAssetAtPath(_oldBuildVisAssetPath, typeof(VectorLayerVisualizerObject));
            var viz = (VectorLayerVisualizer) vizObj.ConstructLayerVisualizer(GetMapInformation(), null);
            
            Measure.Method(() => { RunOldBuildings(viz); })
                .WarmupCount(5)
                .MeasurementCount(SampleCount)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }

        [Test, Performance, Order(4)]
        public void SingleBuildingVisualizerOldChamfer()
        {
            var vizObj = (VectorLayerVisualizerObject) AssetDatabase.LoadAssetAtPath(_oldBuildVisAssetPath, typeof(VectorLayerVisualizerObject));
            var viz = (VectorLayerVisualizer) vizObj.ConstructLayerVisualizer(GetMapInformation(), null);

            Measure.Method(() => { RunOldBuildings(viz); })
                .WarmupCount(0)
                .MeasurementCount(5)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();

        }
        
        private void RunOldBuildings(VectorLayerVisualizer viz)
        {
            var decompressed = Compression.Decompress(buffer);
            var tt = new VectorTile.VectorTile(decompressed, false);
            viz.ClearCaches();
            var layer = tt.GetLayer("building");
            //if (tt.TryGetLayer("building", out var layer))
            {
                viz.CreateMesh(tileId, layer);
            }
        }
    }
}