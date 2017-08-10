//-----------------------------------------------------------------------
// <copyright file="TileTest.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------


// TODO: figure out how run tests outside of Unity with .NET framework, something like '#if !UNITY'
#if UNITY_EDITOR
#if UNITY_5_6_OR_NEWER


namespace Mapbox.MapboxSdkCs.UnitTest
{


	using Mapbox.Map;
	using Mapbox.Platform;
	using NUnit.Framework;
#if UNITY_5_6_OR_NEWER
	using System.Collections;
	using UnityEngine.TestTools;
#endif


	[TestFixture]
	internal class TileTest
	{


		private FileSource _fs;


		[SetUp]
		public void SetUp()
		{
#if UNITY_5_6_OR_NEWER
			_fs = new FileSource(Unity.MapboxAccess.Instance.Configuration.AccessToken);
#else
			// when run outside of Unity FileSource gets the access token from environment variable 'MAPBOX_ACCESS_TOKEN'
			_fs = new FileSource();
#endif
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator TileLoading()
#else
		[Test]
		public void TileLoading() 
#endif
		{
			byte[] data;

			var parameters = new Tile.Parameters();
			parameters.Fs = _fs;
			parameters.Id = new CanonicalTileId(1, 1, 1);

			var tile = new RawPngRasterTile();
			tile.Initialize(parameters, () => { data = tile.Data; });

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif

			Assert.Greater(tile.Data.Length, 1000);
		}



#if UNITY_5_6_OR_NEWER
		[UnityTest]
		public IEnumerator States()
#else
		[Test]
		public void States() 
#endif
		{
			var parameters = new Tile.Parameters();
			parameters.Fs = _fs;
			parameters.Id = new CanonicalTileId(1, 1, 1);

			var tile = new RawPngRasterTile();
			Assert.AreEqual(Tile.State.New, tile.CurrentState);

			tile.Initialize(parameters, () => { });
			Assert.AreEqual(Tile.State.Loading, tile.CurrentState);

#if UNITY_5_6_OR_NEWER
			IEnumerator enumerator = _fs.WaitForAllRequests();
			while (enumerator.MoveNext()) { yield return null; }
#else
			_fs.WaitForAllRequests();
#endif

			Assert.AreEqual(Tile.State.Loaded, tile.CurrentState);

			tile.Cancel();
			Assert.AreEqual(Tile.State.Canceled, tile.CurrentState);
		}
	}
}

#endif
#endif
