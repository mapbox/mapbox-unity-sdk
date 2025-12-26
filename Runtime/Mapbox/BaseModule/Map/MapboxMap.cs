using System;
using System.Collections;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.BaseModule.Map
{
    /// <summary>
    /// The primary object responsible for controlling and rendering the map.
    /// </summary>
    /// <remarks>
    /// - <see cref="MapInformation"/>: Contains information about the map location, 
    ///   detail level, and basic camera controls.
    /// - <see cref="MapVisualizer"/>: Handles styling and visualization of the specified location.
    /// - <see cref="MapService"/>: Manages interactions with the Mapbox API and data handling.
    /// </remarks>
    [Serializable]
    public sealed class MapboxMap
    {
        [NonSerialized] public IMapInformation MapInformation;
        [NonSerialized] public IMapVisualizer MapVisualizer;
        [NonSerialized] public UnityContext UnityContext;
        [NonSerialized] public TileCover TileCover;
        [NonSerialized] public InitializationStatus Status = InitializationStatus.WaitingForInitialization;

        public MapService MapService { get; private set; }

        public MapboxMap(IMapInformation information, UnityContext unityContext, MapService mapMapService)
        {
            MapInformation = information;
            UnityContext = unityContext;
            TileCover = new TileCover();
            MapService = mapMapService;
        }

        /// <summary>
        /// Initialization method which organizes caches and object pool craeting, meta data loading etc.
        /// </summary>
        /// <returns></returns>
        public IEnumerator Initialize()
        {
            if (Status != InitializationStatus.WaitingForInitialization)
                yield break;

            Status = InitializationStatus.Initializing;
            yield return MapVisualizer.Initialize();
            MapVisualizer.TileLoaded += tile => { TileLoaded(tile); };
            MapVisualizer.TileUnloading += tile => { TileUnloading(tile); };
            
            Status = InitializationStatus.Initialized;
            Initialized();
        }

        /// <summary>
        /// Map update method, which we currently use per-frame, to recalculate the tile cover and run map visualizer on it.
        /// </summary>
        public void MapUpdated()
        {
            MapService.TileCover(MapInformation, TileCover);
            MapVisualizer.Load(TileCover);
        }

        /// <summary>
        /// Provides a controlled method for jumping to specific locations on the map.
        /// Unlike standard frame-by-frame map updates, this method ensures precise 
        /// control over the transition process.
        /// </summary>
        /// <param name="callback">An optional callback method to execute upon completion.</param>
        /// <remarks>
        /// - Uses the location already in the settings, useful for initial map load then runtime location changes.
        /// - Triggers events at the start and end of the loading process, useful for showing 
        ///   or hiding a loading screen.
        /// - Suspends per-frame map updates during the loading phase and resumes them 
        ///   once loading is complete.
        /// </remarks>
        public void LoadMapView(Action callback = null)
        {
            Runnable.Instance.StartCoroutine(LoadMapViewCoroutine(MapInformation.LatitudeLongitude, () =>
            {
                callback?.Invoke();
            }));
        }
        
        /// <summary>
        /// Provides a controlled method for jumping to specific locations on the map.
        /// Unlike standard frame-by-frame map updates, this method ensures precise 
        /// control over the transition process.
        /// </summary>
        /// <param name="targetLocation">The destination coordinates as a <see cref="LatitudeLongitude"/> instance.</param>
        /// <param name="callback">An optional callback method to execute upon completion.</param>
        /// <remarks>
        /// - Takes a LatitudeLongitude for new map location.
        /// - Triggers events at the start and end of the loading process, useful for showing 
        ///   or hiding a loading screen.
        /// - Suspends per-frame map updates during the loading phase and resumes them 
        ///   once loading is complete.
        /// </remarks>
        public void LoadMapView(LatitudeLongitude targetLocation, Action callback = null)
        {
            Runnable.Instance.StartCoroutine(LoadMapViewCoroutine(targetLocation, () =>
            {
                callback?.Invoke();
            }));
        }
        
        public IEnumerator LoadMapViewCoroutine(Action callback = null)
        {
            yield return LoadMapViewCoroutine(MapInformation.LatitudeLongitude, callback);
        }

        /// <summary>
        /// Provides a controlled method for jumping to specific locations on the map.
        /// Unlike standard frame-by-frame map updates, this method ensures precise 
        /// control over the transition process.
        /// 
        /// </summary>
        /// <param name="targetLocation">The destination coordinates as a <see cref="LatitudeLongitude"/> instance.</param>
        /// <param name="callback">An optional callback method to execute upon completion.</param>
        /// <returns>
        /// An <see cref="IEnumerator"/> that can be used for coroutine-based execution.
        /// This allows the loading process to be handled asynchronously.
        /// </returns>
        /// <remarks>
        /// - Returns an IEnumerator to let developers create and control their own coroutine.
        /// - Does not pause per-frame updates.
        /// - Does not trigger start and finished events.
        /// </remarks>
        public IEnumerator LoadMapViewCoroutine(LatitudeLongitude targetLocation, Action callback = null)
        {
            Status = InitializationStatus.LoadingView;
            Debug.Log($"MapboxMap: LoadMapView started. Target={targetLocation.Latitude:F5},{targetLocation.Longitude:F5}");
            LoadViewStarting();
            MapInformation.SetInformation(targetLocation);
            
            MapService.TileCover(MapInformation, TileCover);
            Debug.Log($"MapboxMap: TileCover size={TileCover.Tiles.Count}");
            yield return MapVisualizer.LoadTileCoverToMemory(TileCover);
            MapVisualizer.LoadSnapshot(TileCover);
            
            LoadViewCompleted();
            Status = InitializationStatus.ReadyForUpdates;
            Debug.Log("MapboxMap: LoadMapView completed.");
            callback?.Invoke();
        }

        public IEnumerator LoadTileCoverToMemory(TileCover cover)
        {
            yield return MapVisualizer.LoadTileCoverToMemory(cover);
        }
        
        /// <summary>
        /// Change the map core settings.
        /// If the per-frame updates are enabled, new settings will be applied next frame.
        /// </summary>
        /// <param name="latlng">Location to show</param>
        /// <param name="zoom">Zoom level of the map</param>
        /// <param name="pitch">Pitch value of the camera</param>
        /// <param name="bearing">Bearing value of the camera</param>
        public void ChangeView(LatitudeLongitude? latlng = null, float? zoom = null, float? pitch = null, float? bearing = null)
        {
            MapInformation.SetInformation(latlng, zoom, pitch, bearing);
        }
        
        public void OnDestroy()
        {
            MapVisualizer?.OnDestroy();
            MapService.OnDestroy();
        }

        public void UpdateTileCover() => MapService.TileCover(MapInformation, TileCover);
        
        /// <summary>
        /// All modules are initialized, you can get&use any object and/or register to events safely.
        /// </summary>
        public Action Initialized = () => {};
        /// <summary>
        /// Load view procedure is starting. Status is set to InitializationStatus.LoadingView and per-frame updates
        /// are suspended until this procedure finishes.
        /// </summary>
        public Action LoadViewStarting = () => { };
        /// <summary>
        /// Load view procedure is finished. Status is set to InitializationStatus.ReadyForUpdates and per-frame updates
        /// will continue.
        /// </summary>
        public Action LoadViewCompleted = () => { };
        /// <summary>
        /// Map tile finished loading with targeted detail level data. This tile isn't temporary anymore, it'll be in
        /// ActiveTiles list.
        /// </summary>
        public Action<UnityMapTile> TileLoaded = (tile) => { };
        /// <summary>
        /// Map tile unloading event fires for tiles that are still in active tiles list but not in the latest tileCover.
        /// UnityMapTile object attached to event will be pooled after the event call.
        /// </summary>
        public Action<UnityMapTile> TileUnloading = (tile) => { };
    }
}
