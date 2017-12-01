//-----------------------------------------------------------------------
// <copyright file="BboxToVector2dBoundsConverterTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.MapboxSdkCs.UnitTest
{


	using Mapbox.Platform;
	using Mapbox.Tokens;
	using Mapbox.Unity;
	using NUnit.Framework;



	[TestFixture]
	internal class TokenTest
	{


		private FileSource _fs;


		[SetUp]
		public void SetUp()
		{
			_fs = new FileSource(MapboxAccess.Instance.Configuration.AccessToken);
		}


		[Test]
		public void RetrieveConfigToken()
		{
			MapboxTokenApi tokenApi = new MapboxTokenApi(_fs);
			MapboxToken token = tokenApi.Retrieve(MapboxAccess.Instance.Configuration.AccessToken);

			Assert.AreEqual(MapboxTokenStatus.TokenValid, token.Status, "Config token is not valid");
		}



	}
}