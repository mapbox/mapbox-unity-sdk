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
	using ued=UnityEngine.Debug;
	using UnityEngine.TestTools;


	[TestFixture]
	internal class SQLiteCacheTest
	{
		

		private const string _dbName = "UNITTEST.db";
		// tileset names
		private const string TS_NOOVERWRITE = "NoOverwrite";
		private const string TS_FORCEVERWRITE = "ForceOverwrite";
		private const string TS_CONCURRENT1 = "concurrent1";
		private const string TS_CONCURRENT2 = "concurrent2";
		private const string TS_CONCURRENT3 = "concurrent3";
		private const string TS_CONCURRENT4 = "concurrent4";
		private SQLiteCache _cache;
		private string _className;
		private HashSet<CanonicalTileId> _tileIds;


		[OneTimeSetUp]
		public void Setup()
		{
			_className = this.GetType().Name;

			Runnable.EnableRunnableInEditor();

			Vector2d southWest = new Vector2d(48.2174, 16.3662);
			Vector2d northEast = new Vector2d(48.2310, 16.3877);
			Vector2dBounds bounds = new Vector2dBounds(southWest, northEast);
			_tileIds = TileCover.Get(bounds, 19);


			// delete cache from previous runs
			string dbFullPath = SQLiteCache.GetFullDbPath(_dbName);
			if (File.Exists(dbFullPath)) { File.Delete(dbFullPath); }

			_cache = new SQLiteCache(3000, _dbName);
		}


		[OneTimeTearDown]
		public void Cleanup()
		{
			if (null != _cache)
			{
				// TODO: remove comment to cleanup properly after test run
				//_cache.Clear();
				_cache.Dispose();
				_cache = null;
			}
		}


		[Test, Order(1)]
		public void InsertSameTileNoOverwrite()
		{
			string methodName = _className + "." + new StackFrame().GetMethod().Name;
			List<long> elapsed = simpleInsert(TS_NOOVERWRITE, false);
			logTime(methodName, elapsed);
		}


		[Test, Order(2)]
		public void InsertSameTileForceOverwrite()
		{
			string methodName = _className + "." + new StackFrame().GetMethod().Name;
			List<long> elapsed = simpleInsert(TS_FORCEVERWRITE, true);
			logTime(methodName, elapsed);
		}


		[UnityTest, Order(3)]
		public IEnumerator ConcurrentTilesetInsert()
		{

			ued.LogFormat("{0} tiles", _tileIds.Count);

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
			foreach (CanonicalTileId tileId in _tileIds)
			{
				foreach (string tilesetName in tilesetNames)
				{
					CacheItem ci = _cache.Get(tilesetName, tileId);
					Assert.NotNull(ci, "tileset '{0}': {1} not found in cache", tilesetName, tileId);
					Assert.NotNull(ci.Data, "tileset '{0}': {1} tile data is null", tilesetName, tileId);
					Assert.NotZero(ci.Data.Length, "tileset '{0}': {1} data length is 0", tilesetName, tileId);
				}
			}
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
					Data = Enumerable.Repeat((byte)0x20, 100 * 1024).ToArray(),
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

	}
}
