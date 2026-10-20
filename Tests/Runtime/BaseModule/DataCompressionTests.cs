using System.Collections;
using System.Text;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests
{
    public class DataCompressionTests
    {
        private ResilientWebRequestFileSource _fs;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.LoadConfigurationCoroutine(false);
            _fs = new ResilientWebRequestFileSource(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
        }
    
        [Test]
        public void Empty()
        {
            var buffer = new byte[] { };
            Assert.AreEqual(buffer, Compression.Decompress(buffer));
        }

        [Test]
        public void NotCompressed()
        {
            var buffer = Encoding.ASCII.GetBytes("foobar");
            Assert.AreEqual(buffer, Compression.Decompress(buffer));
        }
    
        [UnityTest]
        public IEnumerator Corrupt()
        {
            var buffer = new byte[] { };
            var response = _fs.Request("https://api.mapbox.com/v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf", (response) =>
            {
                if (response.HasError)
                {
                    Debug.LogError(response.ExceptionsAsString);
                }
                buffer = response.Data;
                Assert.Greater(buffer.Length, 30);
                buffer[10] = 0;
                buffer[20] = 0;
                buffer[30] = 0;
                Assert.AreEqual(buffer, Compression.Decompress(buffer));
            });
            yield return TestWait.While(() => !response.IsCompleted, "Corrupt: !response.IsCompleted");
        }
    
        [UnityTest]
        public IEnumerator Decompress() 
        {
            var buffer = new byte[] { };
            var response = _fs.Request("https://api.mapbox.com/v4/mapbox.mapbox-streets-v7/0/0/0.vector.pbf", (response) =>
            {
                if (response.HasError)
                {
                    Debug.LogError(response.ExceptionsAsString);
                }
                buffer = response.Data;
        
                // tiles are automatically decompressed during HttpRequest on full .Net framework
                // not on .NET Core / UWP / Unity
#if UNITY_EDITOR_OSX && UNITY_IOS
        Assert.AreEqual(buffer.Length, Compression.Decompress(buffer).Length); // EditMode on OSX
#elif UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID) // PlayMode tests in Editor
        Debug.Log("EditMode tests in Editor");
		// LessOrEqual (not Less) so the test passes whether the response arrives
		// gzipped (decompression makes it strictly larger) or already
		// transparently decompressed by UnityWebRequest / the server (in which
		// case decompression is a no-op and the sizes match).
		Assert.LessOrEqual(buffer.Length, Compression.Decompress(buffer).Length);
#elif !UNITY_EDITOR && (UNITY_EDITOR_OSX || UNITY_IOS || UNITY_ANDROID) // PlayMode tests on device
		Debug.Log("PlayMode tests on device");
		Assert.AreEqual(buffer.Length, Compression.Decompress(buffer).Length);
#elif NETFX_CORE
		Assert.Less(buffer.Length, Compression.Decompress(buffer).Length);
#else
                Assert.AreEqual(buffer.Length, Compression.Decompress(buffer).Length);
#endif
            });
            yield return TestWait.While(() => !response.IsCompleted, "Decompress: !response.IsCompleted");
        }

    }
}