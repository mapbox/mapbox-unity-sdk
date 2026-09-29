# Recentering Location-Based Maps

In a location-based game the player walks around the real world while the map stays put. The map's world origin is its centre, so every step moves the player farther from `(0, 0, 0)`. Unity stores positions as 32-bit floats, and their precision drops as values grow. A player several kilometres from the origin sees jittering meshes, shimmering shadows and a shaky camera.

`ShiftMapForDistance` (in `Mapbox.LocationModule.Helpers`) fixes this by recentering the map on the player whenever the player gets too far from the centre.

---

## Setup

Add `ShiftMapForDistance` to any GameObject and set:

| Field | What it does |
|---|---|
| `Map` | The `MapBehaviourCore` to recenter. |
| `LocationProviderFactory` | The source of location updates. The component listens to its `DefaultLocationProvider`. |
| `ShiftMapDistance` | How far the player may get from the map centre before a recenter. The value is in Web Mercator units, which equal metres at the equator and shrink with latitude (about half a metre at 60°). `500` works well for walking games. |
| `TransformsToShift` | World-space objects that must stay over the same geographic spot when the map recenters. Optional. See below. |

Both `LocationBasedGame` sample scenes (`LocationGame`, `LocationGameWithPois`) include this component on `Map/MapUtility/LocationModule`. Their `TransformsToShift` lists three objects:

- `MapboxAstronaut`: the player, and also the transform the scene's `TransformBasedTileProvider` follows.
- `CharTarget`: the point the astronaut walks toward, which `SnapTransformToLocationProvider` sets from each location fix.
- `CameraRoot`: the camera rig.

---

## What a recenter does

When a location fix lands farther than `ShiftMapDistance` from the map centre, the component makes that fix the new centre by calling `MapboxMap.ChangeView(location)`. Nothing else about the view changes: zoom, pitch, bearing and scale stay the same.

The tiles reposition themselves, and so does anything parented under a tile. **Nothing else in the scene moves.** Every geographic point now sits at its old position minus an offset, so an object that was over a street corner before the recenter is suddenly floating somewhere else. There are two ways to move such objects.

### 1. `TransformsToShift` (Inspector)

List the objects here and the component shifts them for you. Use it for:

- the player character,
- the camera rig,
- **any transform a tile provider follows**, such as the one assigned to `TransformBasedTileProvider`. This last one matters most (see [Order of operations](#order-of-operations)).

List root objects only. A child of a listed object moves with its parent, and listing both shifts the child twice.

Objects that `SnapTransformToLocationProvider` positions can be listed or left out. It assigns positions absolutely on every location update, so either way works.

### 2. `OnRecentered` (code)

Subscribe to the event for anything spawned or tracked from code, such as markers, route lines, other players' avatars, or cached world positions:

```csharp
using System.Collections.Generic;
using Mapbox.LocationModule.Helpers;
using UnityEngine;

public class MarkerManager : MonoBehaviour
{
    public ShiftMapForDistance Recenter;
    private readonly List<Transform> _markers = new List<Transform>();

    private void OnEnable()  => Recenter.OnRecentered += HandleRecentered;
    private void OnDisable() => Recenter.OnRecentered -= HandleRecentered;

    private void HandleRecentered(MapRecenteredArgs args)
    {
        // Markers live under MapRoot, placed with ConvertLatLngToPosition,
        // so they're in map-local space.
        foreach (var marker in _markers)
        {
            marker.localPosition -= args.LocalOffset;
        }
    }
}
```

`MapRecenteredArgs` carries:

| Member | Meaning |
|---|---|
| `PreviousCenter` | The map centre before the recenter. |
| `NewCenter` | The map centre after it, which is the location fix that triggered it. |
| `LocalOffset` | How far every geographic point moved in `MapRoot` local space. Subtract it from the `localPosition` of objects under `MapRoot`. Y is always 0. |
| `WorldOffset` | The same movement in world space. Subtract it from the `position` of objects outside `MapRoot`. |

The two offsets are identical when `MapRoot` is at the world origin with no rotation, for example in a default scene. They differ when `MapRoot` is transformed, for example when it is parented under an AR anchor. In that case, pick the one that matches the space your object lives in.

Instead of subtracting an offset, you can place objects again from their stored coordinates. By the time the event fires, `MapInformation` already uses the new centre:

```csharp
private void HandleRecentered(MapRecenteredArgs args)
{
    foreach (var poi in _pois)
    {
        poi.Transform.localPosition = _map.MapInformation.ConvertLatLngToPosition(poi.LatLng);
    }
}
```

Both approaches give the same result. Subtracting the offset is cheaper, while placing again also corrects drift if your objects were ever positioned approximately.

---

## Order of operations

On a recenter, everything below runs synchronously, inside the location provider's `OnLocationUpdated`:

1. The offset is computed against the old centre.
2. `TransformsToShift` are shifted.
3. `ChangeView` moves the centre. Tiles reposition, and **the tile cover for the new centre is built**.
4. `OnRecentered` fires.

Step 3 is why tile-provider transforms belong in `TransformsToShift` and not in an `OnRecentered` handler. `TransformBasedTileProvider` reads its transform's position while the cover is built. If that transform is shifted only in step 4, the map loads tiles around a spot one offset away from the player. The wrong cover then stays until something forces a redraw.

Handlers run after step 3, so they see the map, its tiles and `TransformsToShift` all in the new frame.

---

## Things to keep in mind

- **Anything parented under a tile has already moved.** Don't shift it again.
- **The offset belongs to one recenter.** It is not a running total. If you store it and apply it later, make sure the map scale hasn't changed in the meantime. Recentering itself never changes the scale.
- **Recentering is rare.** It happens once each time the player crosses the `ShiftMapDistance` threshold, not on every location update. A `ShiftMapDistance` of `500` means one recenter per roughly 500 m walked, which is cheap. A very small value would call `ChangeView` often and reload tiles each time.
- **Interpolating or smoothed objects** (Cinemachine damping, `TrailRenderer`, interpolated `Rigidbody`) jump when their transform is shifted. Reset their smoothing state in an `OnRecentered` handler if the jump is visible.
- **The first location fix** usually triggers a recenter if the map started at its configured default location. That puts the map on the player, which is normally what you want.

See also [`CoordinateConversions.md`](CoordinateConversions.md) for local versus world space, and [`ChangingMapLocation.md`](ChangingMapLocation.md) for `ChangeView`.
