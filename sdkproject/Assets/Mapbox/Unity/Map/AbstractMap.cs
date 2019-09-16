using Mapbox.Platform.Cache;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.Map.Strategies;
using Mapbox.Unity.Map.TileProviders;

namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Unity.Utilities;
	using Utils;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Globalization;

	/// <summary>
	/// Abstract map.
	/// This is the main monobehavior which controls the map. It controls the visualization of map data.
	/// Abstract map encapsulates the image, terrain and vector sources and provides a centralized interface to control the visualization of the map.
	/// </summary>
	[ExecuteInEditMode]
	public class AbstractMap : MonoBehaviour, IMap
	{
		#region Private Fields

		[SerializeField] private MapOptions _options = new MapOptions();
		[SerializeField] private bool _initializeOnStart = true;
		[SerializeField] protected ImageryLayer _imagery = new ImageryLayer();
		[SerializeField] protected TerrainLayer _terrain = new TerrainLayer();
		[SerializeField] protected VectorLayer _vectorData = new VectorLayer();
		[SerializeField] protected AbstractTileProvider _tileProvider;
		[SerializeField] protected HashSet<UnwrappedTileId> _currentExtent;
		[SerializeField] protected EditorPreviewOptions _previewOptions = new EditorPreviewOptions();
		private List<UnwrappedTileId> tilesToProcess;

		protected AbstractMapVisualizer _mapVisualizer;
		protected float _unityTileSize = 1;
		protected bool _worldHeightFixed = false;
		protected MapboxAccess _fileSource;
		protected int _initialZoom;
		protected Vector2d _centerLatitudeLongitude;
		protected Vector2d _centerMercator;
		protected float _worldRelativeScale;
		protected Vector3 _mapScaleFactor;

		protected Vector3 _cachedPosition;
		protected Quaternion _cachedRotation;
		protected Vector3 _cachedScale = Vector3.one;
		#endregion

		#region Properties

		public bool IsEditorPreviewEnabled
		{
			get
			{
				return _previewOptions.isPreviewEnabled;
			}
			set
			{
				_previewOptions.isPreviewEnabled = value;
			}
		}

		public AbstractMapVisualizer MapVisualizer
		{
			get
			{
				if(_mapVisualizer == null)
				{
					_mapVisualizer = ScriptableObject.CreateInstance<MapVisualizer>();
				}
				return _mapVisualizer;
			}
			set
			{
				_mapVisualizer = value;
			}
		}

		public AbstractTileProvider TileProvider
		{
			get
			{
				return _tileProvider;
			}
			set
			{
				if (_tileProvider != null)
				{
					_tileProvider.ExtentChanged -= OnMapExtentChanged;
				}
				_tileProvider = value;
				if (_tileProvider != null)
				{
					_tileProvider.ExtentChanged += OnMapExtentChanged;
				}
			}
		}

		/// <summary>
		/// The map options.
		/// Options to control the behaviour of the map like location,extent, scale and placement.
		/// </summary>
		public MapOptions Options
		{
			get
			{
				return _options;
			}
			set
			{
				_options = value;
			}
		}

		/// <summary>
		/// Options to control the imagery component of the map.
		/// </summary>
		[NodeEditorElement("Layers")]
		public IImageryLayer ImageLayer
		{
			get
			{
				return _imagery;
			}
		}

		/// <summary>
		/// Options to control the terrain/ elevation component of the map.
		/// </summary>
		[NodeEditorElement("Layers")]
		public ITerrainLayer Terrain
		{
			get
			{
				return _terrain;
			}
		}

		/// <summary>
		/// The vector data.
		/// Options to control the vector data component of the map.
		/// Adds a vector source and visualizers to define the rendering behaviour of vector data layers.
		/// </summary>
		[NodeEditorElement("Layers")]
		public IVectorDataLayer VectorData
		{
			get
			{
				return _vectorData;
			}
		}

		public Vector2d CenterLatitudeLongitude
		{
			get
			{
				return _centerLatitudeLongitude;
			}
		}

		public Vector2d CenterMercator
		{
			get
			{
				return _centerMercator;
			}
		}

		public float WorldRelativeScale
		{
			get
			{
				return _worldRelativeScale;
			}
		}

		public float UnityTileSize
		{
			get
			{
				return _unityTileSize;
			}
		}

		/// <summary>
		/// Gets the absolute zoom of the tiles being currently rendered.
		/// <seealso cref="Zoom"/>
		/// </summary>
		/// <value>The absolute zoom.</value>
		public int AbsoluteZoom
		{
			get
			{
				return (int)Math.Floor(Options.locationOptions.zoom);
			}
		}

		/// <summary>
		/// Gets the current zoom value of the map.
		/// Use <c>AbsoluteZoom</c> to get the zoom level of the tileset.
		/// <seealso cref="AbsoluteZoom"/>
		/// </summary>
		/// <value>The zoom.</value>
		public float Zoom
		{
			get
			{
				return Options.locationOptions.zoom;
			}
		}

		public void SetZoom(float zoom)
		{
			Options.locationOptions.zoom = zoom;
		}

		/// <summary>
		/// Gets the initial zoom at which the map was initialized.
		/// This parameter is useful in calculating the scale of the tiles and the map.
		/// </summary>
		/// <value>The initial zoom.</value>
		public int InitialZoom
		{
			get
			{
				return _initialZoom;
			}
		}


		public Transform Root
		{
			get
			{
				return transform;
			}
		}

		/// <summary>
		/// Setting to trigger map initialization in Unity's Start method.
		/// if set to false, Initialize method should be called explicitly to initialize the map.
		/// </summary>
		public bool InitializeOnStart
		{
			get
			{
				return _initializeOnStart;
			}
			set
			{
				_initializeOnStart = value;
			}
		}

		public HashSet<UnwrappedTileId> CurrentExtent
		{
			get
			{
				return _currentExtent;
			}
		}

		/// <summary>
		/// Gets the loading texture used as a placeholder while the image tile is loading.
		/// </summary>
		/// <value>The loading texture.</value>
		public Texture2D LoadingTexture
		{
			get
			{
				return _options.loadingTexture;
			}
		}

		/// <summary>
		/// Gets the tile material used for map tiles.
		/// </summary>
		/// <value>The tile material.</value>
		public Material TileMaterial
		{
			get
			{
				return _options.tileMaterial;
			}
		}

		public Type ExtentCalculatorType
		{
			get
			{
				return TileProvider.GetType();
			}
		}

		#endregion

		#region Public Methods
		/// <summary>
		/// Initialize the map using the specified latLon and zoom.
		/// Map will automatically get initialized in the <c>Start</c> method.
		/// Use this method to explicitly initialize the map and disable intialize on <c>Start</c>
		/// </summary>
		/// <returns>The initialize.</returns>
		/// <param name="latLon">Lat lon.</param>
		/// <param name="zoom">Zoom.</param>
		public virtual void Initialize(Vector2d latLon, int zoom)
		{
			_initializeOnStart = false;
			if (_options == null)
			{
				_options = new MapOptions();
			}
			_options.locationOptions.latitudeLongitude = String.Format(CultureInfo.InvariantCulture, "{0},{1}", latLon.x, latLon.y);
			_options.locationOptions.zoom = zoom;

			SetUpMap();
		}

		protected virtual void Update()
		{
			if (Application.isEditor && !Application.isPlaying && IsEditorPreviewEnabled == false)
			{
				return;
			}
			if (TileProvider != null)
			{
				TileProvider.UpdateTileProvider();
			}
		}

		public virtual void UpdateMap()
		{
			UpdateMap(Conversions.StringToLatLon(_options.locationOptions.latitudeLongitude), Zoom);
		}

		public virtual void UpdateMap(Vector2d latLon)
		{
			UpdateMap(latLon, Zoom);
		}

		public virtual void UpdateMap(float zoom)
		{
			UpdateMap(Conversions.StringToLatLon(_options.locationOptions.latitudeLongitude), zoom);
		}

		/// <summary>
		/// Updates the map.
		/// Use this method to update the location of the map.
		/// Update method should be used when panning, zooming or changing location of the map.
		/// This method avoid startup delays that might occur on re-initializing the map.
		/// </summary>
		/// <param name="latLon">LatitudeLongitude.</param>
		/// <param name="zoom">Zoom level.</param>
		public virtual void UpdateMap(Vector2d latLon, float zoom)
		{
			if (Application.isEditor && !Application.isPlaying && !IsEditorPreviewEnabled)
			{
				return;
			}

			//so map will be snapped to zero using next new tile loaded
			_worldHeightFixed = false;
			float differenceInZoom = 0.0f;
			bool isAtInitialZoom = false;
			// Update map zoom, if it has changed.
			if (Math.Abs(Zoom - zoom) > Constants.EpsilonFloatingPoint)
			{
				SetZoom(zoom);
			}

			// Compute difference in zoom. Will be used to calculate correct scale of the map.
			differenceInZoom = Zoom - InitialZoom;
			isAtInitialZoom = (differenceInZoom - 0.0 < Constants.EpsilonFloatingPoint);

			//Update center latitude longitude
			var centerLatitudeLongitude = latLon;
			double xDelta = centerLatitudeLongitude.x;
			double zDelta = centerLatitudeLongitude.y;

			xDelta = xDelta > 0 ? Mathd.Min(xDelta, Mapbox.Utils.Constants.LatitudeMax) : Mathd.Max(xDelta, -Mapbox.Utils.Constants.LatitudeMax);
			zDelta = zDelta > 0 ? Mathd.Min(zDelta, Mapbox.Utils.Constants.LongitudeMax) : Mathd.Max(zDelta, -Mapbox.Utils.Constants.LongitudeMax);

			//Set Center in Latitude Longitude and Mercator.
			SetCenterLatitudeLongitude(new Vector2d(xDelta, zDelta));
			Options.scalingOptions.scalingStrategy.SetUpScaling(this);
			Options.placementOptions.placementStrategy.SetUpPlacement(this);

			//Scale the map accordingly.
			if (Math.Abs(differenceInZoom) > Constants.EpsilonFloatingPoint || isAtInitialZoom)
			{
				_mapScaleFactor = Vector3.one * Mathf.Pow(2, differenceInZoom);
				Root.localScale = _mapScaleFactor;
			}

			//Update Tile extent.
			if (TileProvider != null)
			{
				TileProvider.UpdateTileExtent();
			}

			if (OnUpdated != null)
			{
				OnUpdated();
			}
		}

		private void Reset()
		{
			DisableEditorPreview();
		}

		/// <summary>
		/// Resets the map.
		/// Use this method to reset the map.
		/// </summary>
		[ContextMenu("ResetMap")]
		public void ResetMap()
		{
			if(_previewOptions.isPreviewEnabled)
			{
				DisableEditorPreview();
				EnableEditorPreview();
			}
			else
			{
				MapOnAwakeRoutine();
				MapOnStartRoutine(false);
			}
		}

		public bool IsAccessTokenValid
		{
			get
			{
				bool isAccessTokenValid = false;
				try
				{
					var accessTokenCheck = Unity.MapboxAccess.Instance;
					if (Unity.MapboxAccess.Instance.Configuration == null || string.IsNullOrEmpty(Unity.MapboxAccess.Instance.Configuration.AccessToken))
					{
						return false;
					}

					isAccessTokenValid = true;
				}
				catch (System.Exception)
				{
					isAccessTokenValid = false;
				}
				return isAccessTokenValid;
			}
		}
		#endregion

		#region Private/Protected Methods

		private void OnEnable()
		{
			tilesToProcess = new List<UnwrappedTileId>();
			if (_options.tileMaterial == null)
			{
				_options.tileMaterial = new Material(Shader.Find("Standard"));
			}

			if (_options.loadingTexture == null)
			{
				_options.loadingTexture = new Texture2D(1, 1);
			}
		}

		// TODO: implement IDisposable, instead?
		protected virtual void OnDestroy()
		{
			if (TileProvider != null)
			{
				TileProvider.ExtentChanged -= OnMapExtentChanged;
			}
			_mapVisualizer.ClearMap();
			_mapVisualizer.Destroy();
		}

		protected virtual void Awake()
		{
			if (_previewOptions.isPreviewEnabled == true)
			{
				DisableEditorPreview();
				_previewOptions.isPreviewEnabled = false;
			}
			MapOnAwakeRoutine();
		}

		protected virtual void Start()
		{
			MapOnStartRoutine();
		}

		private void MapOnAwakeRoutine()
		{
			// Destroy any ghost game objects.
			DestroyChildObjects();
			// Setup a visualizer to get a "Starter" map.

			if(_mapVisualizer == null)
			{
				_mapVisualizer = ScriptableObject.CreateInstance<MapVisualizer>();
			}
			_mapVisualizer.OnTileFinished += (s) =>
			{
				OnTileFinished(s);
			};
		}

		public void DestroyChildObjects()
		{
			int destroyChildStartIndex = transform.childCount - 1;
			for (int i = destroyChildStartIndex; i >= 0; i--)
			{
				transform.GetChild(i).gameObject.Destroy();
			}
		}

		private void MapOnStartRoutine(bool coroutine = true)
		{
			if (Application.isPlaying)
			{
				if(coroutine)
				{
					StartCoroutine("SetupAccess");
				}
				if (_initializeOnStart)
				{
					SetUpMap();
				}
			}
		}

		private void EnableDisablePreview(object sender, EventArgs e)
		{
			if (!Application.isPlaying)
			{
				if (_previewOptions.isPreviewEnabled)
				{

					EnableEditorPreview();
				}
				else
				{

					DisableEditorPreview();
				}
			}
		}

		public void EnableEditorPreview()
		{
			_cachedPosition = transform.position;
			_cachedRotation = transform.rotation;
			_cachedScale = transform.localScale;

			SetUpMap();
			if (OnEditorPreviewEnabled != null)
			{
				OnEditorPreviewEnabled();
			}
		}

		public void DisableEditorPreview()
		{
			_imagery.UpdateLayer -= OnImageOrTerrainUpdateLayer;
			_terrain.UpdateLayer -= OnImageOrTerrainUpdateLayer;
			_vectorData.SubLayerRemoved -= OnVectorDataSubLayerRemoved;
			_vectorData.SubLayerAdded -= OnVectorDataSubLayerAdded;
			_vectorData.UpdateLayer -= OnVectorDataUpdateLayer;
			_vectorData.UnbindAllEvents();
			if(_mapVisualizer != null)
			{
				_mapVisualizer.ClearMap();
			}
			DestroyTileProvider();

			if (OnEditorPreviewDisabled != null)
			{
				OnEditorPreviewDisabled();
			}

			transform.position = _cachedPosition;
			transform.rotation = _cachedRotation;
			transform.localScale = _cachedScale;
		}

		public void DestroyTileProvider()
		{
			var tileProvider = TileProvider ?? gameObject.GetComponent<AbstractTileProvider>();
			if (_options.extentOptions.extentType != MapExtentType.Custom && tileProvider != null)
			{
				tileProvider.gameObject.Destroy();
				_tileProvider = null;
			}
		}

		protected IEnumerator SetupAccess()
		{
			_fileSource = MapboxAccess.Instance;

			yield return new WaitUntil(() => MapboxAccess.Configured);
		}
		/// <summary>
		/// Sets up map.
		/// This method uses the mapOptions and layer properties to setup the map to be rendered.
		/// Override <c>SetUpMap</c> to write custom behavior to map setup.
		/// </summary>
		protected virtual void SetUpMap()
		{
			SetPlacementStrategy();

			SetScalingStrategy();

			SetTileProvider();

			if (_imagery == null)
			{
				_imagery = new ImageryLayer();
			}
			_imagery.Initialize();

			if (_terrain == null)
			{
				_terrain = new TerrainLayer();
			}
			_terrain.Initialize();

			if (_vectorData == null)
			{
				_vectorData = new VectorLayer();
			}
			_vectorData.Initialize();

			_mapVisualizer.Factories = new List<AbstractTileFactory>
			{
				_terrain.Factory,
				_imagery.Factory,
				_vectorData.Factory
			};

			InitializeMap(_options);
		}

		protected virtual void TileProvider_OnTileAdded(UnwrappedTileId tileId)
		{
			var tile = _mapVisualizer.LoadTile(tileId);
			if (Options.placementOptions.snapMapToZero && !_worldHeightFixed)
			{
				_worldHeightFixed = true;
				if (tile.HeightDataState == MeshGeneration.Enums.TilePropertyState.Loaded)
				{
					ApplySnapWorldToZero(tile);
				}
				else
				{
					tile.OnHeightDataChanged += (s) => { ApplySnapWorldToZero(tile); };
				}
			}
		}

		protected virtual void TileProvider_OnTileRemoved(UnwrappedTileId tileId)
		{
			_mapVisualizer.DisposeTile(tileId);
		}

		protected virtual void TileProvider_OnTileRepositioned(UnwrappedTileId tileId)
		{
			_mapVisualizer.RepositionTile(tileId);
		}

		protected void SendInitialized()
		{
			OnInitialized();
		}

		/// <summary>
		/// Apply Snap World to Zero setting by moving map in Y Axis such that
		/// center of the given tile will be at y=0.
		/// </summary>
		/// <param name="referenceTile">Tile to use for Y axis correction.</param>
		private void ApplySnapWorldToZero(UnityTile referenceTile)
		{
			if (_options.placementOptions.snapMapToZero)
			{
				var h = referenceTile.QueryHeightData(.5f, .5f);
				Root.transform.localPosition = new Vector3(
					Root.transform.position.x,
					-h,
					Root.transform.position.z);
			}
			else
			{
				Root.transform.localPosition = new Vector3(
					Root.transform.position.x,
					0,
					Root.transform.position.z);
			}
		}

		/// <summary>
		/// Initializes the map using the mapOptions.
		/// </summary>
		/// <param name="options">Options.</param>
		protected virtual void InitializeMap(MapOptions options)
		{
			Options = options;
			_worldHeightFixed = false;
			_fileSource = MapboxAccess.Instance;
			_centerLatitudeLongitude = Conversions.StringToLatLon(options.locationOptions.latitudeLongitude);
			_initialZoom = (int)options.locationOptions.zoom;

			options.scalingOptions.scalingStrategy.SetUpScaling(this);
			options.placementOptions.placementStrategy.SetUpPlacement(this);


			//Set up events for changes.
			_imagery.UpdateLayer += OnImageOrTerrainUpdateLayer;
			_terrain.UpdateLayer += OnImageOrTerrainUpdateLayer;

			_vectorData.SubLayerRemoved += OnVectorDataSubLayerRemoved;
			_vectorData.SubLayerAdded += OnVectorDataSubLayerAdded;
			_vectorData.UpdateLayer += OnVectorDataUpdateLayer;

			_options.locationOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				UpdateMap();
			};

			_options.extentOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				OnTileProviderChanged();
			};

			_options.extentOptions.defaultExtents.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				if (Application.isEditor && !Application.isPlaying && IsEditorPreviewEnabled == false)
				{
					Debug.Log("defaultExtents");
					return;
				}
				if (TileProvider != null)
				{
					TileProvider.UpdateTileExtent();
				}
			};

			_options.placementOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				SetPlacementStrategy();
				UpdateMap();
			};

			_options.scalingOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				SetScalingStrategy();
				UpdateMap();
			};

			_mapVisualizer.Initialize(this, _fileSource);
			TileProvider.Initialize(this);

			SendInitialized();

			TileProvider.UpdateTileExtent();
		}

		private void SetScalingStrategy()
		{
			switch (_options.scalingOptions.scalingType)
			{
				case MapScalingType.WorldScale:
					_options.scalingOptions.scalingStrategy = new MapScalingAtWorldScaleStrategy();
					break;
				case MapScalingType.Custom:
					_options.scalingOptions.scalingStrategy = new MapScalingAtUnityScaleStrategy();
					break;
			}
		}

		private void SetPlacementStrategy()
		{
			switch (_options.placementOptions.placementType)
			{
				case MapPlacementType.AtTileCenter:
					_options.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
				case MapPlacementType.AtLocationCenter:
					_options.placementOptions.placementStrategy = new MapPlacementAtLocationCenterStrategy();
					break;
				default:
					_options.placementOptions.placementStrategy = new MapPlacementAtTileCenterStrategy();
					break;
			}
		}

		private void SetTileProvider()
		{
			if (_options.extentOptions.extentType != MapExtentType.Custom)
			{
				ITileProviderOptions tileProviderOptions = _options.extentOptions.GetTileProviderOptions();
				string tileProviderName = "TileProvider";
				// Setup tileprovider based on type.
				switch (_options.extentOptions.extentType)
				{
					case MapExtentType.CameraBounds:
						{
							if (TileProvider != null)
							{
								if (!(TileProvider is QuadTreeTileProvider))
								{
									TileProvider.gameObject.Destroy();
									var go = new GameObject(tileProviderName);
									go.transform.parent = transform;
									TileProvider = go.AddComponent<QuadTreeTileProvider>();
								}
							}
							else
							{
								var go = new GameObject(tileProviderName);
								go.transform.parent = transform;
								TileProvider = go.AddComponent<QuadTreeTileProvider>();
							}
							break;
						}
					case MapExtentType.RangeAroundCenter:
						{
							if (TileProvider != null)
							{
								TileProvider.gameObject.Destroy();
								var go = new GameObject(tileProviderName);
								go.transform.parent = transform;
								TileProvider = go.AddComponent<RangeTileProvider>();
							}
							else
							{
								var go = new GameObject(tileProviderName);
								go.transform.parent = transform;
								TileProvider = go.AddComponent<RangeTileProvider>();
							}
							break;
						}
					case MapExtentType.RangeAroundTransform:
						{
							if (TileProvider != null)
							{
								if (!(TileProvider is RangeAroundTransformTileProvider))
								{
									TileProvider.gameObject.Destroy();
									var go = new GameObject(tileProviderName);
									go.transform.parent = transform;
									TileProvider = go.AddComponent<RangeAroundTransformTileProvider>();
								}
							}
							else
							{
								var go = new GameObject(tileProviderName);
								go.transform.parent = transform;
								TileProvider = go.AddComponent<RangeAroundTransformTileProvider>();
							}
							break;
						}
					default:
						break;
				}
				TileProvider.SetOptions(tileProviderOptions);
			}
			else
			{
				TileProvider = _tileProvider;
			}
		}

		private void TriggerTileRedrawForExtent(ExtentArgs currentExtent)
		{
			var _activeTiles = _mapVisualizer.ActiveTiles;
			_currentExtent = new HashSet<UnwrappedTileId>(currentExtent.activeTiles);

			if (tilesToProcess == null)
			{
				tilesToProcess = new List<UnwrappedTileId>();
			}
			else
			{
				tilesToProcess.Clear();
			}
			foreach (var item in _activeTiles)
			{
				if (TileProvider.Cleanup(item.Key))
				{
					tilesToProcess.Add(item.Key);
				}
			}

			if (tilesToProcess.Count > 0)
			{
				OnTilesDisposing(tilesToProcess);

				foreach (var t2r in tilesToProcess)
				{
					TileProvider_OnTileRemoved(t2r);
				}
			}

			foreach (var tile in _activeTiles)
			{
				// Reposition tiles in case we panned.
				TileProvider_OnTileRepositioned(tile.Key);
			}

			tilesToProcess.Clear();
			foreach (var tile in _currentExtent)
			{
				if (!_activeTiles.ContainsKey(tile))
				{
					tilesToProcess.Add(tile);
				}
			}

			if (tilesToProcess.Count > 0)
			{
				OnTilesStarting(tilesToProcess);
				foreach (var tileId in tilesToProcess)
				{
					_mapVisualizer.State = ModuleState.Working;
					TileProvider_OnTileAdded(tileId);
				}
			}
		}

		private void OnMapExtentChanged(object sender, ExtentArgs currentExtent)
		{
			TriggerTileRedrawForExtent(currentExtent);
		}

		private void OnImageOrTerrainUpdateLayer(object sender, System.EventArgs eventArgs)
		{
			LayerUpdateArgs layerUpdateArgs = eventArgs as LayerUpdateArgs;
			if (layerUpdateArgs != null)
			{
				_mapVisualizer.UpdateTileForProperty(layerUpdateArgs.factory, layerUpdateArgs);
				if (layerUpdateArgs.effectsVectorLayer)
				{
					RedrawVectorDataLayer();
				}
				OnMapRedrawn();
			}
		}

		private void RedrawVectorDataLayer()
		{
			_mapVisualizer.UnregisterTilesFrom(_vectorData.Factory);
			_vectorData.UnbindAllEvents();
			_vectorData.UpdateFactorySettings();
			_mapVisualizer.ReregisterTilesTo(_vectorData.Factory);
		}

		private void OnVectorDataSubLayerRemoved(object sender, EventArgs eventArgs)
		{
			VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

			if (layerUpdateArgs.visualizer != null)
			{
				_mapVisualizer.RemoveTilesFromLayer((VectorTileFactory)layerUpdateArgs.factory, layerUpdateArgs.visualizer);
			}
			OnMapRedrawn();
		}

		private void OnVectorDataSubLayerAdded(object sender, EventArgs eventArgs)
		{
			RedrawVectorDataLayer();
			OnMapRedrawn();
		}

		private void OnVectorDataUpdateLayer(object sender, System.EventArgs eventArgs)
		{

			VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

			if (layerUpdateArgs.visualizer != null)
			{
				//We have a visualizer. Update only the visualizer.
				//No need to unload the entire factory to apply changes.
				_mapVisualizer.UnregisterAndRedrawTilesFromLayer((VectorTileFactory)layerUpdateArgs.factory, layerUpdateArgs.visualizer);
			}
			else
			{
				//We are updating a core property of vector section.
				//All vector features need to get unloaded and re-created.
				RedrawVectorDataLayer();
			}
			OnMapRedrawn();
		}

		private void OnTileProviderChanged()
		{
			if (Application.isEditor && !Application.isPlaying && IsEditorPreviewEnabled == false)
			{
				Debug.Log("extentOptions");
				return;
			}

			SetTileProvider();
			TileProvider.Initialize(this);
			if (IsEditorPreviewEnabled)
			{
				TileProvider.UpdateTileExtent();
			}
		}

		#endregion

		#region Conversion and Height Query Methods
		private Vector3 GeoToWorldPositionXZ(Vector2d latitudeLongitude)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for.
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));
			var worldPos = Conversions.GeoToWorldPosition(latitudeLongitude, CenterMercator, WorldRelativeScale * scaleFactor).ToVector3xz();
			return Root.TransformPoint(worldPos);
		}

		protected virtual float QueryElevationAtInternal(Vector2d latlong, out float tileScale)
		{
			var _meters = Conversions.LatLonToMeters(latlong.x, latlong.y);
			UnityTile tile;
			bool foundTile = MapVisualizer.ActiveTiles.TryGetValue(Conversions.LatitudeLongitudeToTileId(latlong.x, latlong.y, (int)Zoom), out tile);
			if (foundTile)
			{
				tileScale = tile.TileScale;
				var _rect = tile.Rect;
				return tile.QueryHeightData((float)((_meters - _rect.Min).x / _rect.Size.x), (float)((_meters.y - _rect.Max.y) / _rect.Size.y));
			}
			else
			{
				tileScale = 1f;
				return 0f;
			}

		}

		/// <summary>
		/// Converts a latitude longitude into map space position.
		/// </summary>
		/// <returns>Position in map space.</returns>
		/// <param name="latitudeLongitude">Latitude longitude.</param>
		/// <param name="queryHeight">If set to <c>true</c> will return the terrain height(in Unity units) at that point.</param>
		public virtual Vector3 GeoToWorldPosition(Vector2d latitudeLongitude, bool queryHeight = true)
		{
			Vector3 worldPos = GeoToWorldPositionXZ(latitudeLongitude);

			if (queryHeight)
			{
				//Query Height.
				float tileScale = 1f;
				float height = QueryElevationAtInternal(latitudeLongitude, out tileScale);

				// Apply height inside the unity tile space
				UnityTile tile;
				if (MapVisualizer.ActiveTiles.TryGetValue(Conversions.LatitudeLongitudeToTileId(latitudeLongitude.x, latitudeLongitude.y, (int)Zoom), out tile))
				{
					if (tile != null)
					{
						// Calculate height in the local space of the tile gameObject.
						// Height is aligned with the y axis in local space.
						// This also helps us avoid scale values when setting the height.
						var localPos = tile.gameObject.transform.InverseTransformPoint(worldPos);
						localPos.y = height;
						worldPos = tile.gameObject.transform.TransformPoint(localPos);
					}
				}
			}

			return worldPos;
		}

		/// <summary>
		/// Converts a position in map space into a laitude longitude.
		/// </summary>
		/// <returns>Position in Latitude longitude.</returns>
		/// <param name="realworldPoint">Realworld point.</param>
		public virtual Vector2d WorldToGeoPosition(Vector3 realworldPoint)
		{
			// For quadtree implementation of the map, the map scale needs to be compensated for.
			var scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));

			return (Root.InverseTransformPoint(realworldPoint)).GetGeoPosition(CenterMercator, WorldRelativeScale * scaleFactor);
		}

		/// <summary>
		/// Queries the real world elevation data in Unity units at a given latitude longitude.
		/// </summary>
		/// <returns>The height data.</returns>
		/// <param name="latlong">Latlong.</param>
		public virtual float QueryElevationInUnityUnitsAt(Vector2d latlong)
		{
			float tileScale = 1f;
			return QueryElevationAtInternal(latlong, out tileScale);
		}

		/// <summary>
		/// Queries the real world elevation data in Meters at a given latitude longitude.
		/// </summary>
		/// <returns>The height data.</returns>
		/// <param name="latlong">Latlong.</param>
		public virtual float QueryElevationInMetersAt(Vector2d latlong)
		{
			float tileScale = 1f;
			float height = QueryElevationAtInternal(latlong, out tileScale);
			return (height / tileScale);
		}
		#endregion

		#region Map Property Related Changes Methods
		public virtual void SetCenterMercator(Vector2d centerMercator)
		{
			_centerMercator = centerMercator;
		}

		public virtual void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
		{
			_options.locationOptions.latitudeLongitude = string.Format("{0}, {1}", centerLatitudeLongitude.x, centerLatitudeLongitude.y);
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}

		public virtual void SetWorldRelativeScale(float scale)
		{
			_worldRelativeScale = scale;
		}

		public virtual void SetLoadingTexture(Texture2D loadingTexture)
		{
			Options.loadingTexture = loadingTexture;
		}

		public virtual void SetTileMaterial(Material tileMaterial)
		{
			Options.tileMaterial = tileMaterial;
		}

		/// <summary>
		/// Sets the extent type and parameters to control the maps extent.
		/// </summary>
		/// <param name="extentType">Extent type.</param>
		/// <param name="extentOptions">Extent options.</param>
		public virtual void SetExtent(MapExtentType extentType, ExtentOptions extentOptions = null)
		{
			_options.extentOptions.extentType = extentType;

			if (extentOptions != null)
			{
				var currentOptions = _options.extentOptions.GetTileProviderOptions();
				if (currentOptions.GetType() == extentOptions.GetType())
				{
					currentOptions = extentOptions;
				}
			}
			OnTileProviderChanged();
		}

		/// <summary>
		/// Set parameters for current extent calculator strategy.
		/// </summary>
		/// <param name="extentOptions">Parameters to control the map extent.</param>
		public virtual void SetExtentOptions(ExtentOptions extentOptions)
		{
			_options.extentOptions.GetTileProviderOptions().SetOptions(extentOptions);
			_options.extentOptions.defaultExtents.HasChanged = true;
		}

		/// <summary>
		/// Sets the positions of the map's root transform.
		/// Use <paramref name="placementType"/> = <c> MapPlacementType.AtTileCenter</c> to place map root at the center of tile containing the latitude,longitude.
		/// Use <paramref name="placementType"/> = <c> MapPlacementType.AtLocationCenter</c> to place map root at the latitude,longitude.
		/// </summary>
		/// <param name="placementType">Placement type.</param>
		public virtual void SetPlacementType(MapPlacementType placementType)
		{
			_options.placementOptions.placementType = placementType;
			_options.placementOptions.HasChanged = true;
		}

		/// <summary>
		/// Translates map root by the terrain elevation at the center geo location.
		/// Use this method with <c>TerrainWithElevation</c>
		/// </summary>
		/// <param name="active">If set to <c>true</c> active.</param>
		public virtual void SnapMapToZero(bool active)
		{
			_options.placementOptions.snapMapToZero = active;
			_options.placementOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the map to use real world scale for map tile.
		/// Use world scale for AR use cases or applications that need true world scale.
		/// </summary>
		public virtual void UseWorldScale()
		{
			_options.scalingOptions.scalingType = MapScalingType.WorldScale;
			_options.scalingOptions.HasChanged = true;
		}

		/// <summary>
		/// Sets the map to use custom scale for map tiles.
		/// </summary>
		/// <param name="tileSizeInUnityUnits">Tile size in unity units to scale each Web Mercator tile.</param>
		public virtual void UseCustomScale(float tileSizeInUnityUnits)
		{
			_options.scalingOptions.scalingType = MapScalingType.Custom;
			_options.scalingOptions.unityTileSize = tileSizeInUnityUnits;
			_options.scalingOptions.HasChanged = true;
		}

		#endregion

		#region Events
		/// <summary>
		/// Event delegate, gets called after map is initialized
		/// <seealso cref="OnUpdated"/>
		/// </summary>
		public event Action OnInitialized = delegate { };
		/// <summary>
		/// Event delegate, gets called after map is updated.
		/// <c>UpdateMap</c> will trigger this event.
		/// <seealso cref="OnInitialized"/>
		/// </summary>
		public event Action OnUpdated = delegate { };
		public event Action OnMapRedrawn = delegate { };

		/// <summary>
		/// Event delegate, gets called when map preview is enabled
		/// </summary>
		public event Action OnEditorPreviewEnabled = delegate { };
		/// <summary>
		/// Event delegate, gets called when map preview is disabled
		/// </summary>
		public event Action OnEditorPreviewDisabled = delegate { };
		/// <summary>
		/// Event delegate, gets called when a tile is completed.
		/// </summary>
		public event Action<UnityTile> OnTileFinished = delegate { };
		/// <summary>
		/// Event delegate, gets called when new tiles coordinates are registered.
		/// </summary>
		public event Action<List<UnwrappedTileId>> OnTilesStarting = delegate { };
		/// <summary>
		/// Event delegate, gets called before a tile is getting recycled.
		/// </summary>
		public event Action<List<UnwrappedTileId>> OnTilesDisposing = delegate { };
		#endregion
	}
}
