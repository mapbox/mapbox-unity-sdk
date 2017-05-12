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
			for (int x = (int)(_mapController.RefTile.X - _range.x); x <= (_mapController.RefTile.X + _range.z); x++)
			{
				for (int y = (int)(_mapController.RefTile.Y - _range.y); y <= (_mapController.RefTile.Y + _range.w); y++)
				{
					AddTile(new UnwrappedTileId(_mapController.Zoom, x, y));
				}
			}
		}
	}
}
