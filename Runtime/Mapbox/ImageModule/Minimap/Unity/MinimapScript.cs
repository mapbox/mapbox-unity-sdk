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
    /// an <see cref="IMinimapTileSource"/> (<see cref="SourceModule"/> picks a module's data, or call
    /// <see cref="SetTileSource"/> with your own), how they are drawn comes from an
    /// <see cref="IMinimapRenderer"/> (<see cref="SetRenderer"/> to replace the UI one), and where
    /// they go comes from <see cref="MinimapProjection"/>. Read <see cref="View"/> to reach any of
    /// it at runtime.
    /// </summary>
    public class MinimapScript : MonoBehaviour
    {
        public enum CenterMode
        {
            /// <summary>The map's own centre. Right for a camera that recentres the map as it pans.</summary>
            MapCenter,

            /// <summary>A transform's position, such as the player.</summary>
            Transform,

            /// <summary>The ground point a camera is looking at. For free-flying cameras with no player.</summary>
            CameraView
        }

        public enum RotationMode
        {
            /// <summary>North up.</summary>
            None,

            /// <summary>The map's bearing, as set through ChangeView by cameras such as Moving3dCamera.</summary>
            MapBearing,

            /// <summary>A transform's heading, such as a camera rig that rotates around the player.</summary>
            Transform
        }

        [Header("Map")]
        [Tooltip("The map to mirror. The minimap draws the tiles this map has already loaded; it fetches nothing itself.")]
        public MapBehaviourCore Map;

        [Tooltip("Which module's tiles to draw. Leave empty to draw the map's imagery. " +
                 "Pick a terrain module for a height map, or a custom imagery module with Mode = Data Only " +
                 "for a different style than the map shows.")]
        public ModuleConstructorScript SourceModule;

        [Header("Centre")]
        [Tooltip("What the minimap stays centred on. Map Center for a camera that recentres the map as it pans " +
                 "(SlippyMapCamera). Transform to follow an object such as the player. Camera View for a " +
                 "free-flying camera with no player (Moving3dCamera).")]
        public CenterMode Center = CenterMode.MapCenter;

        [Tooltip("The object to stay centred on, such as the player.")]
        public Transform ViewCenter;

        [Tooltip("The camera whose view centre to follow. Centres on the ground point in the middle of its view. " +
                 "Do not use for a camera that orbits a character; the minimap would circle as the camera turns.")]
        public Camera ViewCenterCamera;

        [Header("Rotation")]
        [Tooltip("How the minimap rotates. None keeps north up. Map Bearing follows the map's bearing, set by " +
                 "cameras such as Moving3dCamera. Transform follows a transform's heading, such as a camera rig " +
                 "that rotates around the player.")]
        public RotationMode Rotation = RotationMode.MapBearing;

        [Tooltip("The transform whose heading (Y rotation) the minimap follows, such as the camera rig.")]
        public Transform OrientationTransform;

        [Tooltip("The UI object that rotates. Leave empty to use Content Root's parent, so a frame or compass " +
                 "around the map stays still. Already wired in the prefab.")]
        public RectTransform OrientationRoot;

        [Header("Layout")]
        [Tooltip("Parent of the per-tile images; its centre is the map centre. Leave empty to use this object. " +
                 "Put a Mask on an ancestor for a circular minimap. Already wired in the prefab.")]
        public RectTransform ContentRoot;

        [Tooltip("Pixel size of one tile at the map's zoom, before Zoom Offset. 300 suits a 300-400px panel.")]
        public float TileSize = 300f;

        [Tooltip("Zoom relative to the map. 0 matches the map, negative shows a wider area, positive zooms in. " +
                 "Fractional values work. Only scales the tiles the map already loaded.")]
        public float ZoomOffset = -1.5f;

        [Header("Border")]
        [Tooltip("Optional ring around the panel. Already wired in the prefab; leave empty for no border.")]
        public Image BorderImage;

        [Tooltip("Border width in pixels. 0 hides it.")]
        public float BorderThickness = 7.5f;

        [Tooltip("Border colour. Fully transparent hides it.")]
        public Color BorderColor = Color.white;

        /// <summary>
        /// The live view. Null until the map reports itself initialized. Reach through it to swap
        /// the tile source, adjust the projection, or drive updates yourself.
        /// </summary>
        public MinimapView View { get; private set; }

        private IMinimapRenderer _renderer;
        private IMinimapTileSource _tileSourceOverride;
        private Mapbox.BaseModule.Map.MapboxMap _map;

        /// <summary>
        /// Use a tile source of your own instead of <see cref="SourceModule"/>. Takes effect immediately,
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

            return new ImageryMinimapTileSource();
        }

        /// <summary>
        /// Turns the dropped-in module into a tile source, or returns null to fall back to the
        /// map's imagery.
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
                                   "Falling back to the map's imagery.", this);
                }
                else if (Map != null && SourceModule.gameObject != Map.gameObject)
                {
                    Debug.LogError($"{nameof(MinimapScript)} on '{name}': Source Module " +
                                   $"'{SourceModule.name}' is on a different GameObject than the map " +
                                   $"('{Map.name}'), so the map never built it - modules have to be " +
                                   $"components on the map's own GameObject. Move it onto '{Map.name}' " +
                                   "alongside the other layer modules. Falling back to the map's imagery.",
                                   this);
                }
                else
                {
                    Debug.LogError($"{nameof(MinimapScript)} on '{name}': Source Module " +
                                   $"'{SourceModule.name}' has not built its module yet, so there is " +
                                   "nothing to read. Falling back to the map's imagery.", this);
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
                           "or terrain module here. Falling back to the map's imagery.", this);
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

            switch (Center)
            {
                case CenterMode.CameraView:
                    if (ViewCenterCamera == null)
                    {
                        WarnMissing(nameof(ViewCenterCamera), nameof(CenterMode.CameraView));
                        return null;
                    }
                    // Falls back to the map centre on the frames the centre ray misses the plane.
                    return info.TryGetViewCenterMercator(ViewCenterCamera, out var viewCenter)
                        ? viewCenter
                        : (Mapbox.BaseModule.Data.Vector2d.Vector2d?)null;

                case CenterMode.Transform:
                    if (ViewCenter == null)
                    {
                        WarnMissing(nameof(ViewCenter), nameof(CenterMode.Transform));
                        return null;
                    }
                    // Unity position -> web mercator, the same relation MapInformationStaticMethods
                    // uses: mercator = centre + (x, z) * scale.
                    var local = ViewCenter.position;
                    return new Mapbox.BaseModule.Data.Vector2d.Vector2d(
                        info.CenterMercator.x + local.x * info.Scale,
                        info.CenterMercator.y + local.z * info.Scale);

                default:
                    return null;
            }
        }

        private bool _warnedMissingReference;

        private void WarnMissing(string field, string mode)
        {
            if (_warnedMissingReference)
            {
                return;
            }

            _warnedMissingReference = true;
            Debug.LogWarning($"{nameof(MinimapScript)} on '{name}': {field} is not set but the mode " +
                             $"is {mode}. Using the map centre instead.", this);
        }

        /// <summary>
        /// Bearing in degrees clockwise from north per <see cref="Rotation"/>, or null for north-up.
        /// A transform's heading is taken relative to the map root so a rotated map still reads
        /// correctly. Euler(0, 90, 0) faces +X, which is east, so Unity's Y rotation is
        /// already a bearing and needs no conversion.
        /// </summary>
        private float? ResolveBearing()
        {
            switch (Rotation)
            {
                case RotationMode.MapBearing:
                    return _map.MapInformation.Bearing;
                case RotationMode.Transform:
                    break;
                default:
                    return null;
            }

            if (OrientationTransform == null)
            {
                WarnMissing(nameof(OrientationTransform), nameof(RotationMode.Transform));
                return null;
            }

            var heading = OrientationTransform.eulerAngles.y;
            var mapRoot = _map.UnityContext?.MapRoot;
            if (mapRoot != null)
            {
                heading -= mapRoot.eulerAngles.y;
            }

            return heading;
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
            View.OrientationDegrees = ResolveBearing();

            View.Update();
        }
    }
}
