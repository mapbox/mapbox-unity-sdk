namespace Mapbox.Unity.Map
{
	using System;
	using Mapbox.Unity.Utilities;
	using Mapbox.Utils;
	using UnityEngine;
	using Mapbox.Map;

	public class RangeTileProvider : MonoBehaviour, ITileProvider
	{

		[SerializeField]
		Vector4 _range;




		public event EventHandler<TileStateChangedEventArgs> OnTileAdded;
		public event EventHandler<TileStateChangedEventArgs> OnTileRemoved;

		public void Initialize(UnwrappedTileId referenceTile, int zoom)
		{
			for (int i = (int)(referenceTile.X - _range.x); i <= (referenceTile.X + _range.z); i++)
			{
				for (int j = (int)(referenceTile.Y - _range.y); j <= (referenceTile.Y + _range.w); j++)
				{
					//var tile = new GameObject("Tile - " + i + " | " + j).AddComponent<UnityTile>();
					//_tiles.Add(new Vector2(i, j), tile);
					//tile.Zoom = zoom;
					//tile.RelativeScale = Conversions.GetTileScaleInMeters(0, _zoom) / Conversions.GetTileScaleInMeters((float)lat, _zoom);
					//tile.TileCoordinate = new Vector2(i, j);
					//tile.Rect = Conversions.TileBounds(tile.TileCoordinate, zoom);
					//tile.transform.position = new Vector3((float)(tile.Rect.Center.x - WorldParameters.ReferenceTileRect.Center.x), 0, (float)(tile.Rect.Center.y - WorldParameters.ReferenceTileRect.Center.y));
					//tile.transform.SetParent(Root.transform, false);
					//// send tile
					if (OnTileAdded != null)
					{
						OnTileAdded(this, new TileStateChangedEventArgs() { TileId = new CanonicalTileId(zoom, i, j) });
					}
				}
			}
			//Root = new GameObject("worldRoot");
			//Root.transform.localScale = Vector3.one * WorldParameters.WorldScaleFactor;

			//MapVisualization.Initialize(MapboxAccess.Instance, WorldParameters);
			//_tiles = new Dictionary<Vector2, UnityTile>();
		}

	}
}
