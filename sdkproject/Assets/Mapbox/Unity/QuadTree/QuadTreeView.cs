using System.Collections.Generic;
using Mapbox.Map;

namespace Mapbox.Unity.QuadTree
{
	public class QuadTreeView
	{
		public UnityRectD CenterRect;
		public Dictionary<UnwrappedTileId, UnityRectD> Tiles;

		public QuadTreeView()
		{
			Tiles = new Dictionary<UnwrappedTileId, UnityRectD>();
		}

		public void Clear()
		{
			CenterRect = null;
			Tiles.Clear();
		}
	}
}