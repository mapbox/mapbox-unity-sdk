using UnityEngine;

namespace Mapbox.LandmarksModule.Unity
{
    /// <summary>
    /// Marks a GameObject as a landmark mesh, so a raycast hit can be recognised as one.
    ///
    /// Deliberately data-only, and deliberately without Start or Update. Unity invokes those on
    /// every instance every frame even when the body is empty, and there is one of these per
    /// landmark across the whole tile cover - the empty Update this used to carry was several
    /// hundred no-op calls per frame for nothing.
    /// </summary>
    public class LandmarkComponent : MonoBehaviour
    {
        /// <summary>
        /// The id the tile publishes for this landmark, or null if the tile published none.
        ///
        /// A property rather than a serialized field on purpose: it is assigned as the landmark
        /// loads and means nothing in a saved scene, so there is no reason to show it in the
        /// Inspector or write it to disk. Read it off a raycast hit to answer "which landmark is
        /// this" - it is the same id both the building node and its footprint node carry, so it
        /// also pairs a hit with the footprint the overlap connectors are using.
        /// </summary>
        public string LandmarkId { get; internal set; }
    }
}
