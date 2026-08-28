using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace Mapbox.BaseModuleTests
{
    public class ConversionTests
    {
        [Test]
        public void TileIdToBoundsCenterEqualsToLatlngToTileId()
        {
            var tileId = new CanonicalTileId(16, 37309, 18968);
            var bounds = Conversions.TileIdToBounds(tileId);
            var center = bounds.Center;
            var newTileId = Conversions.LatitudeLongitudeToTileId(center, 16);
            Assert.AreEqual(tileId.ToString(), newTileId.ToString());
        }
        
        [Test]
        public void TileIdToCenterLatLngToTile01ToLatlng()
        {
            var tileId = new CanonicalTileId(16, 37309, 18968);
            var bounds = Conversions.TileIdToBounds(tileId);
            var center = bounds.Center;
            var zeroOne = Conversions.LatitudeLongitudeToInTile01(center, tileId);
            var newLatLng = Conversions.Tile01ToLatitudeLongitude(zeroOne, tileId);
            Assert.AreEqual(center.ToString(), newLatLng.ToString());
        }
        
        [Test]
        public void StringToLatLngToMercatorToLatLng()
        {
            var str = "-77.0295,38.9165";
            var latlng = Conversions.StringToLatLon(str);
            var mercator = Conversions.LatitudeLongitudeToWebMercator(latlng);
            var newLatlng = Conversions.WebMercatorToLatLon(mercator);
            Assert.AreEqual(latlng.Latitude, newLatlng.Latitude, 0.001d);
            Assert.AreEqual(latlng.Longitude, newLatlng.Longitude, 0.001d);
        }
        
        [Test]
        public void WebMercator_RoundTrip_PreservesCoordinatesAcrossWorld()
        {
            // Sample diverse latitudes/longitudes including hemispheres, equator,
            // and near-the-pole values. Mercator only spans ±85.0511° so high
            // latitudes are intentionally near that limit, not beyond.
            var samples = new[]
            {
                new LatitudeLongitude(0, 0),                  // Null Island
                new LatitudeLongitude(40.7128, -74.0060),     // New York
                new LatitudeLongitude(51.5074, -0.1278),      // London
                new LatitudeLongitude(-33.8688, 151.2093),    // Sydney
                new LatitudeLongitude(35.6895, 139.6917),     // Tokyo
                new LatitudeLongitude(-22.9068, -43.1729),    // Rio de Janeiro
                new LatitudeLongitude(85, 179.99),            // near north pole + antimeridian
                new LatitudeLongitude(-85, -179.99),          // near south pole + antimeridian
            };

            foreach (var ll in samples)
            {
                var mercator = Conversions.LatitudeLongitudeToWebMercator(ll);
                var back = Conversions.WebMercatorToLatLon(mercator);

                // 1e-6 degrees ≈ 11cm at the equator. The transform is analytic; this
                // catches sign-flip bugs and the half-circumference offset, not float drift.
                Assert.AreEqual(ll.Latitude, back.Latitude, 1e-6, $"Latitude round-trip failed for {ll}");
                Assert.AreEqual(ll.Longitude, back.Longitude, 1e-6, $"Longitude round-trip failed for {ll}");
            }
        }

        [Test]
        public void LatLngToTileId_RoundTripsThroughTileCenter()
        {
            // Each lat/lng → tile-id → tile-center-lat/lng → tile-id should land
            // back on the same tile. Across multiple zoom levels.
            var samples = new[]
            {
                new LatitudeLongitude(40.7128, -74.0060),     // New York
                new LatitudeLongitude(48.8566, 2.3522),       // Paris
                new LatitudeLongitude(-22.9068, -43.1729),    // Rio
            };

            for (int z = 2; z <= 18; z += 4)
            {
                foreach (var ll in samples)
                {
                    var tileId = Conversions.LatitudeLongitudeToTileId(ll, z);
                    var bounds = Conversions.TileIdToBounds(tileId.Canonical);
                    var tileCenter = bounds.Center;
                    var roundtrip = Conversions.LatitudeLongitudeToTileId(tileCenter, z);

                    Assert.AreEqual(tileId.ToString(), roundtrip.ToString(),
                        $"Tile round-trip failed at z={z} for {ll}");
                }
            }
        }

        [Test]
        public void Tile01_CornersMapToTheMatchingTileCorners()
        {
            // The centre-only tests above pass either way round, because a mirrored longitude
            // cancels at x = 0.5. Corners are where the argument order shows.
            var tileId = new CanonicalTileId(14, 9647, 12321);
            var bounds = Conversions.TileIdToBounds(tileId);

            var southWest = Conversions.Tile01ToLatitudeLongitude(new Vector2(0f, 0f), tileId);
            var northEast = Conversions.Tile01ToLatitudeLongitude(new Vector2(1f, 1f), tileId);

            Assert.AreEqual(bounds.South, southWest.Latitude, 1e-6);
            Assert.AreEqual(bounds.West, southWest.Longitude, 1e-6);
            Assert.AreEqual(bounds.North, northEast.Latitude, 1e-6);
            Assert.AreEqual(bounds.East, northEast.Longitude, 1e-6);
        }

        [Test]
        public void Tile01_RoundTripsAnOffCentrePoint()
        {
            // TileIdToCenterLatLngToTile01ToLatlng round-trips the tile centre, which is the one
            // point that survives a mirrored axis. Use a point well off centre in both axes.
            var tileId = new CanonicalTileId(16, 37309, 18968);
            var bounds = Conversions.TileIdToBounds(tileId);
            var offCentre = new LatitudeLongitude(
                Mathd.Lerp(bounds.South, bounds.North, 0.2),
                Mathd.Lerp(bounds.West, bounds.East, 0.8));

            var zeroOne = Conversions.LatitudeLongitudeToInTile01(offCentre, tileId);
            var roundTripped = Conversions.Tile01ToLatitudeLongitude(zeroOne, tileId);

            Assert.AreEqual(offCentre.Latitude, roundTripped.Latitude, 1e-6);
            Assert.AreEqual(offCentre.Longitude, roundTripped.Longitude, 1e-6);
        }

        [Test]
        public void Tile01_Vector3OverloadReadsUnityTileSpace()
        {
            // (0,0,0) is the north-west corner and z runs to -1 southwards - a different space
            // from the Vector2 overload, deliberately.
            var tileId = new CanonicalTileId(14, 9647, 12321);
            var bounds = Conversions.TileIdToBounds(tileId);

            var northWest = Conversions.Tile01ToLatitudeLongitude(new Vector3(0f, 0f, 0f), tileId);
            var southEast = Conversions.Tile01ToLatitudeLongitude(new Vector3(1f, 0f, -1f), tileId);

            Assert.AreEqual(bounds.North, northWest.Latitude, 1e-6);
            Assert.AreEqual(bounds.West, northWest.Longitude, 1e-6);
            Assert.AreEqual(bounds.South, southEast.Latitude, 1e-6);
            Assert.AreEqual(bounds.East, southEast.Longitude, 1e-6);
        }

        [Test]
        public void Tile01_CenterMapsToTileCenter()
        {
            // (0.5, 0.5) maps to the tile's center regardless of any convention
            // about which corner (0,0) or (1,1) is. Useful invariant; corner
            // mappings are tested separately if needed.
            var tileId = new CanonicalTileId(14, 9647, 12321);
            var bounds = Conversions.TileIdToBounds(tileId);

            var center = Conversions.Tile01ToLatitudeLongitude(new Vector2(0.5f, 0.5f), tileId);

            Assert.AreEqual(bounds.Center.Latitude, center.Latitude, 1e-6);
            Assert.AreEqual(bounds.Center.Longitude, center.Longitude, 1e-6);
        }

        [Test]
        public void TileSizeInUnitySpace_HalvesEveryZoomLevel()
        {
            // Mercator tile pyramid: at each zoom level, tile size halves.
            const float scale = 1f;
            float previousSize = -1f;
            for (int z = 0; z <= 20; z++)
            {
                var size = Conversions.TileSizeInUnitySpace(z, scale);
                Assert.Greater(size, 0f, $"Tile size at z={z} should be positive.");
                if (previousSize > 0)
                {
                    // Allow tiny float drift but the ratio should be ~2.
                    Assert.AreEqual(previousSize / 2f, size, previousSize * 1e-5f,
                        $"Tile size at z={z} should be half of z={z - 1}.");
                }
                previousSize = size;
            }
        }

        [Test]
        public void TileSizeInUnitySpace_ScalesLinearlyWithScale()
        {
            // Doubling the map scale halves the tile size in Unity units.
            var sizeAtScale1 = Conversions.TileSizeInUnitySpace(10, 1f);
            var sizeAtScale2 = Conversions.TileSizeInUnitySpace(10, 2f);
            Assert.AreEqual(sizeAtScale1 / 2f, sizeAtScale2, 1e-4f);
        }

        [Test]
        public void Test_TileEdgeSizeInMercator()
        {
            var testTiles = new[]
            {
                new CanonicalTileId(10, 0, 0),
                new CanonicalTileId(10, 512, 512),
                new CanonicalTileId(10, 1023, 1023),

                new CanonicalTileId(11, 0, 0),
                new CanonicalTileId(11, 900, 400),
                new CanonicalTileId(11, 2047, 2047),

                new CanonicalTileId(12, 0, 0),
                new CanonicalTileId(12, 1500, 1200),
                new CanonicalTileId(12, 4095, 4095),

                new CanonicalTileId(13, 0, 0),
                new CanonicalTileId(13, 4000, 2000),
                new CanonicalTileId(13, 8191, 8191),

                new CanonicalTileId(14, 0, 0),
                new CanonicalTileId(14, 10000, 8000),
                new CanonicalTileId(14, 16383, 16383),

                new CanonicalTileId(15, 0, 0),
                new CanonicalTileId(15, 20000, 15000),
                new CanonicalTileId(15, 32767, 32767),

                new CanonicalTileId(16, 0, 0),
                new CanonicalTileId(16, 40000, 30000),
                new CanonicalTileId(16, 65535, 65535),
            };


            const float epsilon = 1e-4f;
            bool allMatch = true;

            foreach (var tile in testTiles)
            {
                float a = Conversions.CalculateTileEdgeSizeInMercator(tile);
                float b = Conversions.TileEdgeSizeInMercator(tile);

                if (Mathf.Abs(a - b) > epsilon)
                {
                    allMatch = false;
                    Debug.LogError($"Mismatch at Z={tile.Z}, X={tile.X}, Y={tile.Y} → A={a}, B={b}");
                }
            }
        }

    }
}