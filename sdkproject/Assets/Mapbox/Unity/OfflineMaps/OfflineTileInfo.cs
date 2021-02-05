using Mapbox.Map;

public class OfflineTileInfo
{
	public CanonicalTileId CanonicalTileId;
	public OfflineTileType Type;
	public string TilesetId;

	public OfflineTileInfo(UnwrappedTileId tileId, OfflineTileType type, string tilesetId)
	{
		CanonicalTileId = tileId.Canonical;
		Type = type;
		TilesetId = tilesetId;
	}
}