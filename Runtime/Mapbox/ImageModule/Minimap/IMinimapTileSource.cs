using Mapbox.BaseModule.Unity;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Decides what a given tile looks like on the minimap. Implement this to show something other
    /// than the shipped terrain-height or imagery views - a fog-of-war mask, a team-colour overlay,
    /// a custom render texture keyed off the tile id.
    ///
    /// Called every frame for every active tile, so implementations must be cheap and must not
    /// allocate. Return <see cref="MinimapTileTexture.None"/> for a tile whose data has not arrived
    /// yet; that is a normal state and the renderer hides the tile until it has something to draw.
    /// </summary>
    public interface IMinimapTileSource
    {
        MinimapTileTexture GetTexture(UnityMapTile tile);
    }
}
