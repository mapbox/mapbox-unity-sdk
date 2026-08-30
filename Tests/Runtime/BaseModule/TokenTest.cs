using System;
using System.Collections;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using NUnit.Framework;
using UnityEngine.TestTools;
using Mapbox.Tests;

namespace Mapbox.BaseModuleTests
{
    public class TokenTest
    {
        private MapboxTokenApi _tokenApi;
        private string _configAccessToken;
        private Func<string> _configSkuToken;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.LoadConfigurationCoroutine(false);
            _tokenApi = new MapboxTokenApi();
            _configAccessToken = mapboxContext.GetAccessToken();
            _configSkuToken = mapboxContext.GetSkuToken;
        }
        
        [UnityTest]
        public IEnumerator RetrieveConfigToken()
        {
            MapboxToken token = null;

            _tokenApi.Retrieve(
                _configSkuToken,
                _configAccessToken,
                (MapboxToken tok) =>
                {
                    token = tok;
                }
            );

            yield return TestWait.While(() => null == token, "TokenTest: null == token");
            Assert.IsNull(token.ErrorMessage);
            Assert.IsFalse(token.HasError);
            Assert.AreEqual(MapboxTokenStatus.TokenValid, token.Status, "Config token is not valid");
        }
        
        [UnityTest]
        public IEnumerator TokenMalformed()
        {
            MapboxToken token = null;
            _tokenApi.Retrieve(
                _configSkuToken,
                "yada.yada",
                (MapboxToken tok) =>
                {
                    token = tok;
                }
            );

            yield return TestWait.While(() => null == token, "TokenTest: null == token");
            Assert.IsNull(token.ErrorMessage);
            Assert.IsFalse(token.HasError);
            Assert.AreEqual(MapboxTokenStatus.TokenMalformed, token.Status, "token is malformed");
        }
        
        [UnityTest]
        public IEnumerator TokenInvalid()
        {
            MapboxToken token = null;
            _tokenApi.Retrieve(
                _configSkuToken,
                "pk.12345678901234567890123456789012345.0123456789012345678901",
                (MapboxToken tok) =>
                {
                    token = tok;
                }
            );

            yield return TestWait.While(() => null == token, "TokenTest: null == token");
            Assert.IsNull(token.ErrorMessage);
            Assert.IsFalse(token.HasError);
            Assert.AreEqual(MapboxTokenStatus.TokenInvalid, token.Status, "token is invalid");
        }
    }
}