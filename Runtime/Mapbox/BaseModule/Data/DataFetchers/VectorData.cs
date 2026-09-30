using System;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    /// <summary>
    /// Raw vector tile bytes as fetched, plus cache metadata. Holds no decoded form on purpose:
    /// one instance is shared by every module reading the same source, and each decodes into
    /// its own worker-local tile, so nothing here is written after the source publishes it.
    /// </summary>
    [Serializable]
    public class VectorData : MapboxTileData
    {
    }
}
