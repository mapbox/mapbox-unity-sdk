namespace Mapbox.MapboxSdkCs.UnitTest
{
	using Mapbox.Map;
	using Mapbox.Platform.Cache;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using NUnit.Framework;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using ued = UnityEngine.Debug;
	using UnityEngine.TestTools;


	[TestFixture]
	internal class SQLiteCacheTest
	{


		private const string _dbName = "UNITTEST.db";
		// tileset names
		private const string TS_NO_OVERWRITE = "NoOverwrite";
		private const string TS_FORCE_OVERWRITE = "ForceOverwrite";
		private const string TS_CONCURRENT1 = "concurrent1";
		private const string TS_CONCURRENT2 = "concurrent2";
		private const string TS_CONCURRENT3 = "concurrent3";
		private const string TS_CONCURRENT4 = "concurrent4";
		private const string TS_PRUNE = "concurrent4";
		private const string TS_REINIT = "reinit";
		private string[] _allTilesetNames;
		private SQLiteCache _cache;
		private string _className;
		private HashSet<CanonicalTileId> _tileIds;
		// be careful when setting the 'maxTileCount' parameter. when setting too low unwanted pruning might happen.
		private uint _maxCacheTileCount = 6000;


		[OneTimeSetUp]
		public void Init()
		{
			_className = this.GetType().Name;

			Runnable.EnableRunnableInEditor();

			_allTilesetNames = new string[] {
				TS_NO_OVERWRITE
				, TS_FORCE_OVERWRITE
				, TS_CONCURRENT1
				, TS_CONCURRENT2
				, TS_CONCURRENT3
				, TS_CONCURRENT4
				, TS_PRUNE
				, TS_REINIT
			};

			Vector2d southWest = new Vector2d(48.2174, 16.3662);
			Vector2d northEast = new Vector2d(48.2310, 16.3877);
			Vector2dBounds bounds = new Vector2dBounds(southWest, northEast);
			_tileIds = TileCover.Get(bounds, 19);


			// delete cache from previous runs
			string dbFullPath = SQLiteCache.GetFullDbPath(_dbName);
			if (File.Exists(dbFullPath)) { File.Delete(dbFullPath); }

			_cache = new SQLiteCache(_maxCacheTileCount, _dbName);
		}


		[OneTimeTearDown]
		public void Cleanup()
		{
			if (null != _cache)
			{
				//_cache.Clear();
				_cache.Dispose();
				_cache = null;
			}
		}


		[Test, Order(1)]
		public void InsertSameTileNoOverwrite()
		{
			string methodName = _className + "." + new StackFrame().GetMethod().Name;
			List<long> elapsed = simpleInsert(TS_NO_OVERWRITE, false);
			logTime(methodName, elapsed);
			cacheItemAsserts(TS_NO_OVERWRITE, new CanonicalTileId(0, 0, 0));
			Assert.AreEqual(1, _cache.TileCount(TS_NO_OVERWRITE), "tileset {0}: unexpected number of tiles", TS_NO_OVERWRITE);
		}


		[Test, Order(2)]
		public void InsertSameTileForceOverwrite()
		{
			string methodName = _className + "." + new StackFrame().GetMethod().Name;
			List<long> elapsed = simpleInsert(TS_FORCE_OVERWRITE, true);
			logTime(methodName, elapsed);
			cacheItemAsserts(TS_FORCE_OVERWRITE, new CanonicalTileId(0, 0, 0));
			Assert.AreEqual(1, _cache.TileCount(TS_FORCE_OVERWRITE), "tileset {0}: unexpected number of tiles", TS_FORCE_OVERWRITE);
		}


		[UnityTest, Order(3)]
		public IEnumerator ConcurrentTilesetInsert()
		{

			ued.LogFormat("about to insert {0} tiles for each tileset", _tileIds.Count);

			int rIdCr1 = Runnable.Run(InsertCoroutine(TS_CONCURRENT1, false, _tileIds));
			int rIdCr2 = Runnable.Run(InsertCoroutine(TS_CONCURRENT2, false, _tileIds));
			int rIdCr3 = Runnable.Run(InsertCoroutine(TS_CONCURRENT3, false, _tileIds));
			int rIdCr4 = Runnable.Run(InsertCoroutine(TS_CONCURRENT4, false, _tileIds));

			while (Runnable.IsRunning(rIdCr1) || Runnable.IsRunning(rIdCr2) || Runnable.IsRunning(rIdCr3) || Runnable.IsRunning(rIdCr4))
			{
				yield return null;
			}

		}


		[Test, Order(4)]
		public void VerifyTilesFromConcurrentInsert()
		{
			ued.Log("verifying concurrently inserted tiles ...");

			string[] tilesetNames = new string[] { TS_CONCURRENT1, TS_CONCURRENT2, TS_CONCURRENT3, TS_CONCURRENT4 };

			foreach (string tilesetName in tilesetNames)
			{
				Assert.AreEqual(_tileIds.Count, _cache.TileCount(tilesetName), "tileset '{0}' does not contain expected number of tiles", tilesetName);
			}

			foreach (string tilesetName in tilesetNames)
			{
				foreach (CanonicalTileId tileId in _tileIds)
				{
					cacheItemAsserts(tilesetName, tileId);
				}
			}

			ued.Log("all tiles in cache!");
		}


		[Test, Order(5)]
		public void Prune()
		{
			string methodName = _className + "." + new StackFrame().GetMethod().Name;
			HashSet<CanonicalTileId> tileIds = new HashSet<CanonicalTileId>();

			int tiles2Insert = (int)_maxCacheTileCount + (int)_cache.PruneCacheDelta + 1;
			ued.Log(string.Format("about to insert {0} tiles", tiles2Insert));
			for (int x = 0; x < tiles2Insert; x++)
			{
				tileIds.Add(new CanonicalTileId(x, 131205, 18));
			}

			List<long> elapsed = simpleInsert(TS_PRUNE, false, tileIds);
			logTime(methodName, elapsed);
			Assert.AreEqual(_maxCacheTileCount, _cache.TileCount(TS_PRUNE), _cache.PruneCacheDelta, "tileset [{0}]: pruning did not work as expected", TS_PRUNE);
		}


		[Test, Order(6)]
		public void Clear()
		{
			// We still should have tiles in the cache
			long tileCnt = getAllTilesCount();

			// beware 'Assert.Greater' has parameters flipped compared to 'Assert.AreEqual'
			Assert.GreaterOrEqual(tileCnt, _cache.MaxCacheSize, "number of tiles lower than expected");

			_cache.Clear();
			// have to Reinit after Clear()
			_cache.ReInit();

			tileCnt = getAllTilesCount();

			Assert.AreEqual(0, tileCnt, "'Clear()' did not work as expected");
		}


		[Test, Order(7)]
		public void ReInit()
		{
			// after previous 'Clear' there shouldn't be any tiles in cache
			long tileCnt = getAllTilesCount();
			Assert.AreEqual(0, tileCnt, "'Clear()' did not work as expected");
			// insert one tile
			simpleInsert(TS_REINIT, false, itemCount: 1);
			tileCnt = getAllTilesCount();
			Assert.AreEqual(1, tileCnt, "one tile was not inserted");

			_cache.ReInit();

			Assert.AreEqual(1, tileCnt, "tile was lost during 'ReInit()'");
		}


		#region helper methods


		private long getAllTilesCount()
		{
			long tileCnt = 0;
			foreach (string tilesetName in _allTilesetNames)
			{
				tileCnt += _cache.TileCount(tilesetName);
			}
			return tileCnt;
		}


		private void cacheItemAsserts(string tilesetName, CanonicalTileId tileId)
		{
			CacheItem ci = _cache.Get(tilesetName, tileId);
			Assert.NotNull(ci, "tileset '{0}': {1} not found in cache", tilesetName, tileId);
			Assert.NotNull(ci.Data, "tileset '{0}': {1} tile data is null", tilesetName, tileId);
			Assert.NotZero(ci.Data.Length, "tileset '{0}': {1} data length is 0", tilesetName, tileId);
		}


		private IEnumerator InsertCoroutine(string tileSetName, bool forceInsert, HashSet<CanonicalTileId> tileIds = null)
		{
			ued.Log(string.Format("coroutine [{0}] started", tileSetName));
			yield return null;

			List<long> elapsed = simpleInsert(tileSetName, forceInsert, tileIds);

			//List<long> elapsed = new List<long>();
			//foreach (CanonicalTileId tileId in tileIds)
			//{
			//	HashSet<CanonicalTileId> tmpIds = new HashSet<CanonicalTileId>(new CanonicalTileId[] { tileId });
			//	elapsed.AddRange(simpleInsert(tileSetName, forceInsert, tmpIds));
			//	yield return null;
			//}

			ued.Log(string.Format("coroutine [{0}] finished", tileSetName));
			logTime(tileSetName, elapsed);
		}


		private List<long> simpleInsert(string tileSetName, bool forceInsert, HashSet<CanonicalTileId> tileIds = null, int itemCount = 1000)
		{
			if (null != tileIds) { itemCount = tileIds.Count; }

			List<long> elapsed = new List<long>();
			Stopwatch sw = new Stopwatch();

			for (int i = 0; i < itemCount; i++)
			{
				CanonicalTileId tileId = null != tileIds ? tileIds.ElementAt(i) : new CanonicalTileId(0, 0, 0);
				DateTime now = DateTime.UtcNow;
				CacheItem cacheItem = new CacheItem()
				{
					AddedToCacheTicksUtc = now.Ticks,
					// simulate 100KB data
					Data = Enumerable.Repeat((byte)0x58, 100 * 1024).ToArray(),
					ETag = "etag",
					LastModified = now
				};

				sw.Start();
				_cache.Add(tileSetName, tileId, cacheItem, forceInsert);
				sw.Stop();
				elapsed.Add(sw.ElapsedMilliseconds);
				sw.Reset();
			}

			return elapsed;
		}



		private void logTime(string label, List<long> elapsed)
		{
			double overall = elapsed.Sum() / 1000.0;
			double min = elapsed.Min() / 1000.0;
			double max = elapsed.Max() / 1000.0;
			double avg = elapsed.Average() / 1000.0;

			double sum = elapsed.Sum(d => Math.Pow(d - avg, 2));
			double stdDev = (Math.Sqrt((sum) / (elapsed.Count - 1))) / 1000.0;

			ued.Log(string.Format(
				CultureInfo.InvariantCulture
				, "[{0}] {1} items, overall time:{2,6:0.000}s avg:{3,6:0.000}s min:{4,6:0.000}s max:{5,6:0.000}s stdDev:{6,6:0.000}s"
				, label
				, elapsed.Count
				, overall
				, avg
				, min
				, max
				, stdDev
			));
		}


		#endregion


	}
}
