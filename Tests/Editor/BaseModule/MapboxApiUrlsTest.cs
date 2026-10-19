using System;
using Mapbox.BaseModule.Utilities;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mapbox.BaseModuleTests
{
    [TestFixture]
    public class MapboxApiUrlsTest
    {
        private const string AccessToken = "pk.test";

        private static readonly string[] NonMapboxUrls =
        {
            "https://attacker.example/v4/x.png",
            "https://api.mapbox.com.evil.example/v4/x.png",
            "https://evil.example/?redirect=https://api.mapbox.com",
            "https://api.mapbox.com@evil.example/v4/x.png",
            "https://notmapbox.com/"
        };

        private static Func<string> SkuToken()
        {
            return () => "01";
        }

        [SetUp]
        public void SetUp()
        {
            // AppendCredentials warns once per host through a static set that survives between
            // tests, so which test sees a given warning depends on execution order. Log output is
            // therefore not assertable here - every assertion below is on the returned value.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void IsMapboxHost_AcceptsMapboxApiHosts()
        {
            Assert.IsTrue(MapboxApiUrls.IsMapboxHost("https://api.mapbox.com/v4/mapbox.satellite/0/0/0.png"));
            Assert.IsTrue(MapboxApiUrls.IsMapboxHost("https://api.tiles.mapbox.com/3dtiles/v1/x/0/0/0.glb"));
            Assert.IsTrue(MapboxApiUrls.IsMapboxHost("https://events.mapbox.com/events/v2"));
            Assert.IsTrue(MapboxApiUrls.IsMapboxHost("https://mapbox.com/"));
            Assert.IsTrue(MapboxApiUrls.IsMapboxHost("https://api.mapbox.com/v4/mapbox.streets.json?secure"));
        }

        [Test]
        public void IsMapboxHost_RejectsNonMapboxHosts()
        {
            foreach (var url in NonMapboxUrls)
            {
                Assert.IsFalse(MapboxApiUrls.IsMapboxHost(url), "Expected non-Mapbox host: " + url);
            }
        }

        [Test]
        public void IsMapboxHost_RejectsNonHttpsSchemes()
        {
            Assert.IsFalse(MapboxApiUrls.IsMapboxHost("http://api.mapbox.com/v4/x.png"));
            Assert.IsFalse(MapboxApiUrls.IsMapboxHost("mapbox://styles/mapbox/streets-v11"));
        }

        [Test]
        public void IsMapboxHost_RejectsRelativeAndMalformed()
        {
            Assert.IsFalse(MapboxApiUrls.IsMapboxHost("v4/mapbox.satellite/0/0/0.png"));
            Assert.IsFalse(MapboxApiUrls.IsMapboxHost("not a url"));
            Assert.IsFalse(MapboxApiUrls.IsMapboxHost(""));
            Assert.IsFalse(MapboxApiUrls.IsMapboxHost(null));
        }

        [Test]
        public void AppendCredentials_AppendsWhenNoExistingQuery()
        {
            const string url = "https://api.mapbox.com/v4/mapbox.satellite/0/0/0.png";
            var result = new Uri(MapboxApiUrls.AppendCredentials(url, AccessToken, SkuToken()));

            // Compared through Uri rather than as strings because UriBuilder.ToString()
            // emits the default port (":443") under Mono.
            Assert.AreEqual(url, result.GetLeftPart(UriPartial.Path));
            Assert.AreEqual("?access_token=pk.test&sku=01", result.Query);
        }

        [Test]
        public void AppendCredentials_MergesWithExistingQuery()
        {
            const string url = "https://api.mapbox.com/v4/mapbox.streets.json?secure";
            var result = new Uri(MapboxApiUrls.AppendCredentials(url, AccessToken, SkuToken()));

            Assert.AreEqual("https://api.mapbox.com/v4/mapbox.streets.json", result.GetLeftPart(UriPartial.Path));
            Assert.AreEqual("?secure&access_token=pk.test&sku=01", result.Query);
        }

        [Test]
        public void AppendCredentials_LeavesNonMapboxHostUntouched()
        {
            foreach (var url in NonMapboxUrls)
            {
                var result = MapboxApiUrls.AppendCredentials(url, AccessToken, SkuToken());
                Assert.AreEqual(url, result, "Credentials must not be attached to " + url);
            }
        }

        [Test]
        public void AppendCredentials_LeavesUrlUntouchedWhenNoToken()
        {
            const string url = "https://api.mapbox.com/v4/mapbox.satellite/0/0/0.png";

            Assert.AreEqual(url, MapboxApiUrls.AppendCredentials(url, "", SkuToken()));
            Assert.AreEqual(url, MapboxApiUrls.AppendCredentials(url, null, SkuToken()));
        }

        [Test]
        public void AppendCredentials_DoesNotEvaluateSkuTokenWhenSkipped()
        {
            const string mapboxUrl = "https://api.mapbox.com/v4/mapbox.satellite/0/0/0.png";
            const string otherUrl = "https://attacker.example/v4/x.png";

            var evaluated = false;
            Func<string> skuToken = () =>
            {
                evaluated = true;
                return "01";
            };

            MapboxApiUrls.AppendCredentials(otherUrl, AccessToken, skuToken);
            Assert.IsFalse(evaluated, "SKU token must not be evaluated for a non-Mapbox host.");

            MapboxApiUrls.AppendCredentials(mapboxUrl, "", skuToken);
            Assert.IsFalse(evaluated, "SKU token must not be evaluated when there is no access token.");

            MapboxApiUrls.AppendCredentials(mapboxUrl, AccessToken, skuToken);
            Assert.IsTrue(evaluated, "SKU token must be evaluated for a Mapbox host.");
        }
    }
}
