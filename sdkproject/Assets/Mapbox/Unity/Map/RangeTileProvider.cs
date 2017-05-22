namespace Mapbox.Unity.Map
{
	using UnityEngine;
	using Mapbox.Map;

	public class RangeTileProvider : AbstractTileProvider
	{
		[SerializeField]
		Vector4 _range;

		internal override void OnInitialized()
		{
			var centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.Zoom);
			for (int x = (int)(centerTile.X- _range.x); x <= (centerTile.X + _range.z); x++)
			{
				for (int y = (int)(centerTile.Y - _range.y); y <= (centerTile.Y + _range.w); y++)
				{
					AddTile(new UnwrappedTileId(_map.Zoom, x, y));
				}
			}
		}
	}
}
