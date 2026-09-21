using UnityEngine;

namespace Mapbox.LandmarksModule
{
    /// <summary>
    /// One landmark building and the point on the ground it stands at, produced during glTF
    /// instantiation and consumed by <see cref="LandmarkTerrainSnapper"/>.
    ///
    /// The anchor is captured rather than derived later because the glTF node carries it and
    /// nothing downstream can recover it: <c>FlipYZ</c> bakes the node translation into the mesh
    /// vertices, so once instantiation finishes the only record of where a building sits inside its
    /// tile is the geometry itself.
    /// </summary>
    internal struct LandmarkAnchor
    {
        /// <summary>
        /// The building's own GameObject. Its parent chain has unit Y scale (the landmark tile root
        /// uses <c>localScale = (tileSize, 1, tileSize)</c>), so a local Y set on this transform is
        /// world Y one-to-one - no tile-size division, unlike the vector mesh modifiers.
        /// </summary>
        public Transform Transform;

        /// <summary>
        /// Where the building meets the ground, in the 0-1 space an elevation query expects:
        /// x eastward from the tile's west edge, y NORTHWARD FROM THE SOUTH EDGE.
        ///
        /// Note the y flip relative to everything else in the landmark pipeline. Landmark and
        /// terrain geometry both use tile-local z from 0 at the north edge to -1 at the south,
        /// but elevation sampling is documented as texture space with the origin bottom-left, so
        /// this stores 1 + z. It is stored already-flipped so the conversion happens once, here,
        /// instead of at every sampling site - which is where SnapTerrainModifier got it wrong in
        /// one of its two branches.
        /// </summary>
        public Vector2 GroundUv;

        /// <summary>
        /// Outline of the building's footprint in the same flipped 0-1 space as
        /// <see cref="GroundUv"/>, or null when the tile published no footprint for it.
        ///
        /// Used to snap to the lowest point under the building rather than the height at its
        /// centre. A single centre sample leaves the downhill edge hanging in the air on a slope,
        /// which reads far worse than the uphill edge being buried - real foundations cut into a
        /// hill rather than float over it.
        /// </summary>
        public Vector2[] FootprintUvs;

        /// <summary>
        /// The tile's own id for this landmark, used to pair a building node with its footprint
        /// node. Both carry it, under different keys. Pairing by id rather than by list position
        /// matters because a footprint with a hole contributes more than one ring, which silently
        /// breaks index alignment.
        /// </summary>
        public string LandmarkId;
    }
}
