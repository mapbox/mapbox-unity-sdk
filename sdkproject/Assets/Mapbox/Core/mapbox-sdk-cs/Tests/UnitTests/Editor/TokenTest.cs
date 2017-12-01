//-----------------------------------------------------------------------
// <copyright file="TokenTest.cs" company="Mapbox">
//     Copyright (c) 2017 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{


	using Mapbox.Tokens;
	using Mapbox.Unity;
	using NUnit.Framework;
	using System.Collections;
	using UnityEngine.TestTools;


	[TestFixture]
	internal class TokenTest
	{


		private MapboxTokenApi _tokenApi;
		private string _configAccessToken;

		[SetUp]
		public void SetUp()
		{
			_tokenApi = new MapboxTokenApi();
			_configAccessToken = MapboxAccess.Instance.Configuration.AccessToken;
		}


		[UnityTest]
		public IEnumerator RetrieveConfigToken()
		{

			MapboxToken token = null;

			_tokenApi.Retrieve(
				_configAccessToken,
				(MapboxToken tok) =>
				{
					token = tok;
				}
			);

			while (null == token) { yield return null; }

			Assert.IsNull(token.ErrorMessage);
			Assert.IsFalse(token.HasError);
			Assert.AreEqual(MapboxTokenStatus.TokenValid, token.Status, "Config token is not valid");
		}


		[UnityTest]
		public IEnumerator TokenMalformed()
		{

			MapboxToken token = null;

			_tokenApi.Retrieve(
				"yada.yada"
				, (MapboxToken tok) =>
				{
					token = tok;
				}
			);

			while (null == token) { yield return null; }

			Assert.IsNull(token.ErrorMessage);
			Assert.IsFalse(token.HasError);
			Assert.AreEqual(MapboxTokenStatus.TokenMalformed, token.Status, "token is malformed");
		}


		[UnityTest]
		public IEnumerator TokenInvalid()
		{

			MapboxToken token = null;

			_tokenApi.Retrieve(
				"pk.12345678901234567890123456789012345.0123456789012345678901"
				, (MapboxToken tok) =>
				{
					token = tok;
				}
			);

			while (null == token) { yield return null; }

			Assert.IsNull(token.ErrorMessage);
			Assert.IsFalse(token.HasError);
			Assert.AreEqual(MapboxTokenStatus.TokenInvalid, token.Status, "token is invalid");

		}


	}
}