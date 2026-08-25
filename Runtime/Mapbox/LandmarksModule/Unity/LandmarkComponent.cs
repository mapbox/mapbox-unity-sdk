using UnityEngine;

namespace Mapbox.LandmarkModule
{
    /// <summary>
    /// Marks a GameObject as a landmark mesh, so a raycast hit can be recognised as one.
    ///
    /// Deliberately empty and deliberately without Start or Update. Unity invokes those on every
    /// instance every frame even when the body is empty, and there is one of these per landmark
    /// across the whole tile cover - the empty Update this used to carry was several hundred no-op
    /// calls per frame for nothing.
    /// </summary>
    public class LandmarkComponent : MonoBehaviour
    {
    }
}
