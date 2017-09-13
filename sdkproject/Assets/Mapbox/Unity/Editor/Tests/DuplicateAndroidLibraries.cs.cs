namespace Mapbox.Editor.Tests
{
	using NUnit.Framework;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using UnityEngine;
	using Mapbox.Editor.Build;

	[TestFixture]
	internal class AndroidLibraries
	{
		[Test]
		public void Duplicates()
		{
			List<AndroidLibInfo> libInfo = new List<AndroidLibInfo>();
			foreach (var file in Directory.GetFiles(Application.dataPath, "*.jar", SearchOption.AllDirectories))
			{
				libInfo.Add(new AndroidLibInfo(file));
			}
			foreach (var file in Directory.GetFiles(Application.dataPath, "*.aar", SearchOption.AllDirectories))
			{
				libInfo.Add(new AndroidLibInfo(file));
			}

			var stats = libInfo.GroupBy(li => li.BaseFileName).OrderBy(g => g.Key);
			var max = stats.Select(s => s.Count()).Max();

			string msg = string.Empty;
			if (max > 1)
			{
				var x = stats
					.Where(s => s.Count() > 1)
					.SelectMany(a => a.Select(l => l.AssetPath)).ToArray();
				msg = "Duplicate Android libraries found: " + string.Join(Environment.NewLine, x);
			}

			Assert.AreEqual(1, max, msg);
		}
	}
}