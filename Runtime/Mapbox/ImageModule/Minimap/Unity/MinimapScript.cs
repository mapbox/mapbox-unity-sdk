using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace Mapbox.ImageModule.Minimap.Unity
{
    /// <summary>
    /// Drop-in minimap: point it at a map and a UI root and it mirrors the map's tiles every frame.
    ///
    /// This is the wiring only - every decision it makes is delegated. What tiles show comes from
    /// an <see cref="IMinimapTileSource"/> (<see cref="Source"/> picks a shipped one, or call
    /// <see cref="SetTileSource"/> with your own), how they are drawn comes from an
    /// <see cref="IMinimapRenderer"/> (<see cref="SetRenderer"/> to replace the UI one), and where
    /// they go comes from <see cref="MinimapProjection"/>. Read <see cref="View"/> to reach any of
    /// it at runtime.
    /// </summary>
    public class MinimapScript : MonoBehaviour
    {
        public enum MinimapSource
        {
            /// <summary>Encoded elevation raster - reads as a greyscale relief map.</summary>
            TerrainHeight,

            /// <summary>Map imagery, so the minimap matches the map seen from above.</summary>
            Imagery
        }

        [Tooltip("The map to mirror. Any MapBehaviourCore - the same one driving your scene's map. " +
                 "The minimap adds no fetching of its own; it draws the tiles that map already has.")]
        public MapBehaviourCore Map;

        [Tooltip("Parent for the per-tile images, and the thing whose centre is the map centre. " +
                 "Put a Mask or RectMask2D on an ANCESTOR of this to get a circular (or any shaped) " +
                 "minimap. Leave empty to use this object's own RectTransform.")]
        public RectTransform ContentRoot;

        [Tooltip("What gets rotated when Rotate With Bearing is on. Usually Content Root's parent, " +
                 "so a frame or compass drawn around the map does not rotate with it. " +
                 "Leave empty to use Content Root's parent.")]
        public RectTransform OrientationRoot;

        [Tooltip("Optional, and the best choice for a camera that moves rather than recentring the " +
                 "map. The minimap centres on what this camera is looking at - where a ray through " +
                 "the middle of the screen meets the map plane - which is what you want for a " +
                 "pitched or 3D view, because the camera itself sits behind and above that point. " +
                 "Takes priority over View Center. Falls back to View Center, then the map's own " +
                 "centre, on the frames the centre ray misses the plane (a camera at or above the " +
                 "horizon).")]
        public Camera ViewCenterCamera;

        [Tooltip("Optional. What the minimap centres on - normally the camera, or whatever the camera " +
                 "looks at. Leave empty for a camera that recentres the map as it pans (SlippyMapCamera). " +
                 "REQUIRED for a camera that keeps the map origin fixed and moves itself instead " +
                 "(Moving3dCamera, and most 3D/first-person rigs): those never tell the map its centre " +
                 "changed, so without this the minimap holds the start position while the tiles slide " +
                 "off the panel.")]
        public Transform ViewCenter;

        [Tooltip("Optional. Drop a layer module here to read ITS data, instead of whatever the map " +
                 "left on the tile.\n\n" +
                 "This is how the minimap shows a DIFFERENT style from the main map: add a second " +
                 "custom imagery module, set its Mode to Data Only so it does not draw over the map, " +
                 "and drop it here. Satellite on the map, streets on the minimap.\n\n" +
                 "Leave empty to draw whatever the map itself is displaying, chosen by Source below. " +
                 "A terrain module dropped here resolves to the terrain height texture.")]
        public ModuleConstructorScript SourceModule;

        [Tooltip("Which tile texture to draw. Terrain Height needs a terrain module in the scene; " +
                 "Imagery needs an imagery module. For anything else, call SetTileSource in code.")]
        public MinimapSource Source = MinimapSource.TerrainHeight;

        [Tooltip("Pixel size of one tile at the map's current zoom, before zoom offset. " +
                 "300 suits a ~300-400px panel; raise it for a larger minimap, lower it to fit " +
                 "more tiles in the same space.")]
        public float TileSize = 300f;

        [Tooltip("Zoom levels away from the map's zoom. 0 matches the main view exactly. " +
                 "Negative shows a wider area, positive zooms in, and fractional values work. " +
                 "Defaults to -1.5, which shows noticeably more context than the main view. Only " +
                 "scales what is drawn: it does not load tiles the map has not loaded, so a large " +
                 "negative value shows a wider area at coarser detail, not more area.")]
        public float ZoomOffset = -1.5f;

        [Tooltip("Optional ring drawn around the minimap. A uniformly scaled copy of the panel " +
                 "sitting behind the mask, so only the part outside the mask shows. Wired to the " +
                 "prefab's Border image; leave empty for no border.")]
        public Image BorderImage;

        [Tooltip("Border width in pixels on each edge. 0 hides the border. Converted to a uniform " +
                 "scale on the border image, so it stays a ring of even width whatever the panel " +
                 "size - 7.5 on a 300px panel is a scale of 1.05.")]
        public float BorderThickness = 7.5f;

        [Tooltip("Border colour. A fully transparent colour hides the border, same as thickness 0.")]
        public Color BorderColor = Color.white;

        [Tooltip("Rotate the minimap with the map's bearing, so it always faces the way the camera " +
                 "does. Off leaves it north-up, which is usually what you want if the minimap has a " +
                 "compass or labels drawn on it.")]
        public bool RotateWithBearing = true;

        /// <summary>
        /// The live view. Null until the map reports itself initialized. Reach through it to swap
        /// the tile source, adjust the projection, or drive updates yourself.
        /// </summary>
        public MinimapView View { get; private set; }

        private IMinimapRenderer _renderer;
        private IMinimapTileSource _tileSourceOverride;
        private Mapbox.BaseModule.Map.MapboxMap _map;

        /// <summary>
        /// Use a tile source of your own instead of <see cref="Source"/>. Takes effect immediately,
        /// before or after initialization.
        /// </summary>
        public void SetTileSource(IMinimapTileSource tileSource)
        {
            _tileSourceOverride = tileSource;
            if (View != null && tileSource != null)
            {
                View.TileSource = tileSource;
            }
        }

        /// <summary>
        /// Render somewhere other than UI. Call before the map initializes, or after to rebuild
        /// against the new renderer.
        /// </summary>
        public void SetRenderer(IMinimapRenderer renderer)
        {
            if (_renderer != null && View != null)
            {
                View.Clear();
            }

            _renderer = renderer;
            View = null;

            if (_map != null)
            {
                BuildView();
            }
        }

        private void Awake()
        {
            if (Map == null)
            {
                Debug.LogError($"{nameof(MinimapScript)} on '{name}' has no Map assigned; " +
                               "there is nothing for it to mirror and it will stay blank.", this);
                return;
            }

            Map.Initialized += OnMapInitialized;

            // Also handle a map that is already up. Initialized is a plain callback, not a replayed
            // one, so a minimap enabled after the map finished initializing - spawned at runtime,
            // on a panel toggled open later, or simply ordered after the map in the scene - would
            // otherwise wait for an event that has already been and gone, and stay blank forever.
            if (Map.MapboxMap != null)
            {
                OnMapInitialized(Map.MapboxMap);
            }
        }

        private void OnDestroy()
        {
            if (Map != null)
            {
                Map.Initialized -= OnMapInitialized;
            }

            View?.Clear();
        }

        private void OnMapInitialized(Mapbox.BaseModule.Map.MapboxMap map)
        {
            // Both the event and the already-initialized check above can reach this, and only one
            // of them should build anything.
            if (_map == map && View != null)
            {
                return;
            }

            _map = map;
            BuildView();
        }

        private void BuildView()
        {
            var contentRoot = ContentRoot != null ? ContentRoot : transform as RectTransform;
            if (contentRoot == null)
            {
                Debug.LogError($"{nameof(MinimapScript)} on '{name}' needs a RectTransform: assign " +
                               "Content Root, or put this component on a UI object.", this);
                return;
            }

            if (_renderer == null)
            {
                var orientationRoot = OrientationRoot != null
                    ? OrientationRoot
                    : contentRoot.parent as RectTransform;
                _renderer = new RawImageMinimapRenderer(contentRoot, orientationRoot);
            }

            View = new MinimapView(
                _map.MapVisualizer,
                _map.MapInformation,
                _renderer,
                _tileSourceOverride ?? CreateTileSource());
        }

        private IMinimapTileSource CreateTileSource()
        {
            if (SourceModule != null)
            {
                var resolved = ResolveModuleTileSource();
                if (resolved != null)
                {
                    return resolved;
                }
            }

            switch (Source)
            {
                case MinimapSource.Imagery:
                    return new ImageryMinimapTileSource();
                default:
                    return new TerrainHeightMinimapTileSource();
            }
        }

        /// <summary>
        /// Turns the dropped-in module into a tile source, or returns null to fall back to
        /// <see cref="Source"/>.
        /// </summary>
        private IMinimapTileSource ResolveModuleTileSource()
        {
            var implementation = SourceModule.ModuleImplementation;
            if (implementation == null)
            {
                // Distinguish the two reasons, because they need different fixes and the common one
                // is not a timing problem: the map collects modules with GetComponents on its own
                // GameObject, not GetComponentsInChildren, so a module script sitting on any other
                // object is never constructed and its ModuleImplementation stays null forever.
                if (!SourceModule.enabled)
                {
                    // The map skips disabled module scripts when it collects them, so the component
                    // is never constructed at all - not merely inactive.
                    Debug.LogError($"{nameof(MinimapScript)} on \'{name}\': Source Module " +
                                   $"\'{SourceModule.name}\' is disabled, and the map skips disabled " +
                                   "module scripts when it builds them. Enable the component. " +
                                   "Falling back to the Source setting.", this);
                }
                else if (Map != null && SourceModule.gameObject != Map.gameObject)
                {
                    Debug.LogError($"{nameof(MinimapScript)} on '{name}': Source Module " +
                                   $"'{SourceModule.name}' is on a different GameObject than the map " +
                                   $"('{Map.name}'), so the map never built it - modules have to be " +
                                   $"components on the map's own GameObject. Move it onto '{Map.name}' " +
                                   "alongside the other layer modules. Falling back to the Source setting.",
                                   this);
                }
                else
                {
                    Debug.LogError($"{nameof(MinimapScript)} on '{name}': Source Module " +
                                   $"'{SourceModule.name}' has not built its module yet, so there is " +
                                   "nothing to read. Falling back to the Source setting.", this);
                }

                return null;
            }

            // Matches the custom imagery module too, which derives from this - so this needs no
            // reference to the custom imagery assembly, and could not have one: that assembly
            // references this one.
            if (implementation is StaticApiLayerModule imagery)
            {
                return new RasterSourceMinimapTileSource(imagery.RasterSource);
            }

            // Read the module's own source, not the tile's terrain slot: the slot holds whichever
            // module drew it, so going through the tile would ignore which module was dropped in.
            if (implementation is TerrainLayerModule terrain)
            {
                return new RasterSourceMinimapTileSource<Mapbox.BaseModule.Data.DataFetchers.TerrainData>(
                    terrain.RasterSource);
            }

            Debug.LogError($"{nameof(MinimapScript)} on '{name}': Source Module " +
                           $"'{SourceModule.name}' is a {implementation.GetType().Name}, which has no " +
                           "imagery or terrain for a minimap to draw. Drop an imagery, custom imagery " +
                           "or terrain module here. Falling back to the Source setting.", this);
            return null;
        }

        /// <summary>
        /// Where the minimap should be centred, in order of preference: what the camera is looking
        /// at, then <see cref="ViewCenter"/>'s position, then null for the map's own centre.
        ///
        /// The camera comes first because it is the only one of the three that is right for a
        /// pitched view: the camera's own position is behind and above what it is looking at, so
        /// centring on it puts the view centre off toward one edge of the minimap.
        /// </summary>
        private Mapbox.BaseModule.Data.Vector2d.Vector2d? ResolveCenter()
        {
            var info = _map.MapInformation;

            if (ViewCenterCamera != null && info.TryGetViewCenterMercator(ViewCenterCamera, out var viewCenter))
            {
                return viewCenter;
            }

            if (ViewCenter != null)
            {
                // Unity position -> web mercator, the same relation MapInformationStaticMethods uses:
                // mercator = centre + (x, z) * scale.
                var local = ViewCenter.position;
                return new Mapbox.BaseModule.Data.Vector2d.Vector2d(
                    info.CenterMercator.x + local.x * info.Scale,
                    info.CenterMercator.y + local.z * info.Scale);
            }

            return null;
        }

        /// <summary>
        /// Sizes and colours the border ring.
        ///
        /// Thickness is per-edge pixels rather than a raw scale because the border is a uniformly
        /// scaled copy of the panel: growing it by one thickness on every side at once means the
        /// scale depends on the panel's size, which is not something a value in the Inspector should
        /// have to know. Derived from the border's own rect so it stays correct if the border is
        /// sized differently from the panel.
        /// </summary>
        private void ApplyBorder()
        {
            if (BorderImage == null)
            {
                return;
            }

            var width = BorderImage.rectTransform.rect.width;
            if (width > 0f)
            {
                var scale = 1f + (BorderThickness * 2f) / width;
                BorderImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            BorderImage.color = BorderColor;
            BorderImage.enabled = BorderThickness > 0f && BorderColor.a > 0f;
        }

#if UNITY_EDITOR
        // So border tweaks show up in the scene view without entering play mode.
        private void OnValidate()
        {
            ApplyBorder();
        }
#endif

        private void Update()
        {
            // Before the View check: the border is chrome and does not depend on the map being up.
            ApplyBorder();

            if (View == null)
            {
                return;
            }

            View.Projection.TileSize = TileSize;
            View.Projection.ZoomOffset = ZoomOffset;

            // Unity position -> web mercator, the same relation MapInformationStaticMethods uses:
            // mercator = centre + (x, z) * scale. Done directly rather than through
            // ConvertPositionToLatLng so it does not round-trip via lat/lng every frame.
            View.Projection.CenterOverride = ResolveCenter();

            // Plain Bearing, no sign flip or offset needed, and the three conventions that make it
            // so are worth recording because they are easy to second-guess: bearing is clockwise
            // from north (Euler(0, 90, 0) faces +X/east), the panel's +y is north (a tile one step
            // north lands at +tileSize), and a positive UI z-rotation is counter-clockwise
            // (Euler(0, 0, 90) takes +x to +y). Requiring the camera's facing to land at panel-up
            // then gives rotation == bearing exactly.
            View.OrientationDegrees = RotateWithBearing ? _map.MapInformation.Bearing : (float?)null;

            View.Update();
        }
    }
}
