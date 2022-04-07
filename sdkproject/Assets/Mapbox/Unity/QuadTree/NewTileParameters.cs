using System.Collections.Generic;
using Mapbox.Map;

namespace Mapbox.Unity.QuadTree
{
	public class NewTileParameters
	{
		public UnwrappedTileId TileId;
		public UnityRectD UnityRectD;
		public bool InitializeVisible;
		public List<string> Logs;

		public NewTileParameters(UnwrappedTileId tileId, UnityRectD rect, bool visible, List<string> tileLogs)
		{
			TileId = tileId;
			UnityRectD = rect;
			InitializeVisible = visible;
			Logs = tileLogs;
		}
	}
}