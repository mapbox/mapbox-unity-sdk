## Working with the Map Object

When you want to interact with the map (querying elevation, getting the latitude and longitude from the mouse position, or changing the map’s location) you first need to access the map object.

The map system has two parts:  
A MonoBehaviour script that manages the setup in Unity, and the underlying map object that performs the actual operations.  
All interaction from the Unity side begins through the MonoBehaviour. From there, you can retrieve a reference to the map object and store it for later use.

A common approach to obtain and cache the map object is shown below:

```csharp
public class CustomScript : MonoBehaviour
{
    public MapBehaviourCore Core;
    private MapboxMap _map;
    
    private void Awake()
    {
        Core.Initialized += map =>
        {
            _map = map;
        };
    }
}
```


If you plan to use the map object in the Update method, it’s good practice to check its current state before performing operations.
This ensures that your logic only runs once the map is properly initialized and ready.

```csharp
private void Update()
{
    // Ready to work with submodules and their settings
    if (_map != null && _map.Status >= InitializationStatus.Initialized)
    {
        // Example: access map data or modify settings
    }
    
    // Initial view is loaded and ready for user interaction
    if (_map != null && _map.Status >= InitializationStatus.ReadyForUpdates)
    {
        // Example: perform runtime updates or interactions
    }
}
```

This setup guarantees that any scripts depending on the map will only run when the system is prepared, preventing null references and timing issues during initialization.
## Tile zoom levels and uniform grids

`UnityTileProvider` is a quadtree LOD provider. It subdivides tiles that are close to the camera and
leaves distant ones coarse, so **a single frame's tile cover normally mixes zoom levels**. This is the
right default for a map you fly a camera over: near ground stays detailed and the horizon stays cheap.

It is the wrong default if your project assumes a uniform grid — a hex or square world laid over the
map, a per-tile simulation step, or anything that indexes tiles by a fixed `(x, y)` at one zoom. Under
LOD, neighbouring cells arrive at different zooms and your indices stop lining up.

To get a uniform cover, pin both ends of the range to the same value on
`UnityTileProviderSettings`:

```csharp
tileProvider.Settings.MinimumZoomLevel = 14;
tileProvider.Settings.MaximumZoomLevel = 14;
```

Both are also exposed on `UnityTileProviderBehaviour` in the Inspector. With the two equal, every tile
in the cover is at that zoom and the quadtree stops subdividing.

Two caveats:

- `MaximumZoomLevel` is clamped by the map's `AbsoluteZoom` and, internally, to 30. Pinning above the
  map's current zoom will not raise it.
- Setting `MinimumZoomLevel` *above* `MaximumZoomLevel` is a misconfiguration, not a way to force
  detail: tiles are force-split to the absolute cap and the provider logs a warning.

Data tiles are a separate matter from render tiles. Terrain in particular is fetched at a lower zoom
than the tiles that render it and shared across them, so pinning the render zoom does not by itself
give you one data tile per rendered tile.
