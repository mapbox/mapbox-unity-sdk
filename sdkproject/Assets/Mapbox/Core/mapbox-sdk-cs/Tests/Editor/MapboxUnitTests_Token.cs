//-----------------------------------------------------------------------
// <copyright file="TokenTest.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if MAPBOX_EXPERIMENTAL
namespace Mapbox.MapboxSdkCs.UnitTest
{


	using Mapbox.Tokens;
	using Mapbox.Unity;
	using NUnit.Framework;
	using System;
	using System.Collections;
	using UnityEngine.TestTools;


	[TestFixture]
	internal class TokenTest
	{


		private MapboxTokenApi _tokenApi;
		private string _configAccessToken;

		[OneTimeSetUp]
		public void SetUp()
		{
			_tokenApi = MapboxAccess.Instance.TokenValidator;
			_configAccessToken = MapboxAccess.Instance.Configuration.AccessToken;
		}


		[UnityTest]
		public IEnumerator RetrieveConfigToken()
		{
			bool running = true;
			Action asyncWorkaround = async () =>
			{
				MapboxToken token = await _tokenApi.Retrieve(_configAccessToken);

				Assert.IsNull(token.ErrorMessage);
				Assert.IsFalse(token.HasError);
				Assert.AreEqual(MapboxTokenStatus.TokenValid, token.Status, "Config token is not valid");
				running = false;
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}


		[UnityTest]
		public IEnumerator TokenMalformed()
		{
			bool running = true;
			Action asyncWorkaround = async () =>
			{
				MapboxToken token = await _tokenApi.Retrieve("yada.yada");

				Assert.IsNull(token.ErrorMessage);
				Assert.IsFalse(token.HasError);
				Assert.AreEqual(MapboxTokenStatus.TokenMalformed, token.Status, "token is malformed");
				running = false;
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}


		[UnityTest]
		public IEnumerator TokenInvalid()
		{
			bool running = true;
			Action asyncWorkaround = async () =>
			{
				MapboxToken token = await _tokenApi.Retrieve("pk.12345678901234567890123456789012345.0123456789012345678901");

				Assert.IsNull(token.ErrorMessage);
				Assert.IsFalse(token.HasError);
				Assert.AreEqual(MapboxTokenStatus.TokenInvalid, token.Status, "token is invalid");
				running = false;
			};
			asyncWorkaround();

			while (running) { yield return null; }
		}


	}
}
#endif
