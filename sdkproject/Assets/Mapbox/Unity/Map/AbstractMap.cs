namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using Mapbox.Unity.Utilities;
	using Utils;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Data;
	using System.Globalization;

	public interface IUnifiedMap
	{
		//void InitializeMap(MapOptions options);
		void UpdateMap(Vector2d latLon, float zoom);
		void ResetMap();
	}

	public interface IMapScalingStrategy
	{
		void SetUpScaling(AbstractMap map);
	}

	public class MapScalingAtWorldScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(AbstractMap map)
		{
			var scaleFactor = Mathf.Pow(2, (map.AbsoluteZoom - map.InitialZoom));
			map.SetWorldRelativeScale(scaleFactor * Mathf.Cos(Mathf.Deg2Rad * (float)map.CenterLatitudeLongitude.x));
		}
	}

	public class MapScalingAtUnityScaleStrategy : IMapScalingStrategy
	{
		public void SetUpScaling(AbstractMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetWorldRelativeScale((float)(map.Options.scalingOptions.unityTileSize / referenceTileRect.Size.x));
		}
	}

	public interface IMapPlacementStrategy
	{
		void SetUpPlacement(AbstractMap map);
	}

	public class MapPlacementAtTileCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(AbstractMap map)
		{
			var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(map.CenterLatitudeLongitude, map.AbsoluteZoom));
			map.SetCenterMercator(referenceTileRect.Center);
		}
	}

	public class MapPlacementAtLocationCenterStrategy : IMapPlacementStrategy
	{
		public void SetUpPlacement(AbstractMap map)
		{
			map.SetCenterMercator(Conversions.LatLonToMeters(map.CenterLatitudeLongitude));
		}
	}
	/// <summary>
	/// Abstract map.
	/// This is the main monobehavior which controls the map. It controls the visualization of map data.
	/// Abstract map encapsulates the image, terrain and vector sources and provides a centralized interface to control the visualization of the map.
	/// </summary>
	public class AbstractMap : MonoBehaviour, IMap
	{
		/// <summary>
		/// Setting to trigger map initialization in Unity's Start method.
		/// if set to false, Initialize method should be called explicitly to initialize the map.
		/// </summary>
		[SerializeField]
		private bool _initializeOnStart = true;
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
		/// <summary>
		/// The map options.
		/// Options to control the behaviour of the map like location,extent, scale and placement.
		/// </summary>
		[SerializeField]
		private MapOptions _options;
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
		[SerializeField]
		ImageryLayer _imagery = new ImageryLayer();
		[NodeEditorElement("Layers")]
		public ImageryLayer ImageLayer
		{
			get
			{
				return _imagery;
			}
		}
		/// <summary>
		/// Options to control the terrain/ elevation component of the map.
		/// </summary>
		[SerializeField]
		TerrainLayer _terrain = new TerrainLayer();
		[NodeEditorElement("Layers")]
		public TerrainLayer Terrain
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
		[SerializeField]
		VectorLayer _vectorData = new VectorLayer();
		[NodeEditorElement("Layers")]
		public VectorLayer VectorData
		{
			get
			{
				return _vectorData;
			}
		}

		[SerializeField]
		protected AbstractTileProvider _tileProvider;
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
				_tileProvider.ExtentChanged += OnMapExtentChanged;
			}
		}
		[SerializeField]
		protected HashSet<UnwrappedTileId> _currentExtent;
		public HashSet<UnwrappedTileId> CurrentExtent
		{
			get
			{
				return _currentExtent;
			}
		}

		protected AbstractMapVisualizer _mapVisualizer;
		public AbstractMapVisualizer MapVisualizer
		{
			get
			{
				return _mapVisualizer;
			}
			set
			{
				_mapVisualizer = value;
			}
		}

		protected float _unityTileSize = 1;
		public float UnityTileSize
		{
			get
			{
				return _unityTileSize;
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
				return _tileProvider.GetType();
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

		protected int _initialZoom;
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

		protected bool _worldHeightFixed = false;

		protected MapboxAccess _fileSource;

		protected Vector2d _centerLatitudeLongitude;
		public Vector2d CenterLatitudeLongitude
		{
			get
			{
				return _centerLatitudeLongitude;
			}
		}

		protected Vector2d _centerMercator;
		public Vector2d CenterMercator
		{
			get
			{
				return _centerMercator;
			}
		}

		protected float _worldRelativeScale;
		private Vector3 _mapScaleFactor;

		public float WorldRelativeScale
		{
			get
			{
				return _worldRelativeScale;
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

		public Transform Root
		{
			get
			{
				return transform;
			}
		}

		public void SetCenterMercator(Vector2d centerMercator)
		{
			_centerMercator = centerMercator;
		}

		public void SetCenterLatitudeLongitude(Vector2d centerLatitudeLongitude)
		{
			_options.locationOptions.latitudeLongitude = string.Format("{0}, {1}", centerLatitudeLongitude.x, centerLatitudeLongitude.y);
			_centerLatitudeLongitude = centerLatitudeLongitude;
		}

		public void SetWorldRelativeScale(float scale)
		{
			_worldRelativeScale = scale;
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

		protected virtual void Awake()
		{
			// Setup a visualizer to get a "Starter" map.
			_mapVisualizer = ScriptableObject.CreateInstance<MapVisualizer>();
		}

		// Use this for initialization
		protected virtual void Start()
		{
			StartCoroutine("SetupAccess");
			if (_initializeOnStart)
			{
				SetUpMap();
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

			_mapVisualizer.Factories = new List<AbstractTileFactory>();

			_mapVisualizer.Factories.Add(_terrain.Factory);
			_mapVisualizer.Factories.Add(_imagery.Factory);
			_mapVisualizer.Factories.Add(_vectorData.Factory);

			InitializeMap(_options);
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
				// Setup tileprovider based on type.
				switch (_options.extentOptions.extentType)
				{
					case MapExtentType.CameraBounds:
						TileProvider = gameObject.AddComponent<QuadTreeTileProvider>();
						break;
					case MapExtentType.RangeAroundCenter:
						TileProvider = gameObject.AddComponent<RangeTileProvider>();
						break;
					case MapExtentType.RangeAroundTransform:
						TileProvider = gameObject.AddComponent<RangeAroundTransformTileProvider>();
						break;
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
			// Change Map Visualizer state
			_mapVisualizer.State = ModuleState.Working;
			List<UnwrappedTileId> _toRemove = new List<UnwrappedTileId>();
			foreach (var item in _activeTiles)
			{
				if (!_currentExtent.Contains(item.Key))
				{
					_toRemove.Add(item.Key);
				}
			}

			foreach (var t2r in _toRemove)
			{
				TileProvider_OnTileRemoved(t2r);
			}

			foreach (var tile in _activeTiles)
			{
				// Reposition tiles in case we panned.
				TileProvider_OnTileRepositioned(tile.Key);
			}

			foreach (var tile in _currentExtent)
			{
				if (!_activeTiles.ContainsKey(tile))
				{
					TileProvider_OnTileAdded(tile);
				}
			}
		}

		private void OnMapExtentChanged(object sender, ExtentArgs currentExtent)
		{
			TriggerTileRedrawForExtent(currentExtent);
		}

		// TODO: implement IDisposable, instead?
		protected virtual void OnDestroy()
		{
			if (_tileProvider != null)
			{
				_tileProvider.ExtentChanged -= OnMapExtentChanged;
			}

			_mapVisualizer.Destroy();
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

			_imagery.UpdateLayer += (object sender, System.EventArgs eventArgs) =>
			{
				LayerUpdateArgs layerUpdateArgs = eventArgs as LayerUpdateArgs;
				if (layerUpdateArgs != null)
				{
					Debug.Log("<color=red>Image</color>");
					_mapVisualizer.UpdateTileForProperty(layerUpdateArgs.factory, layerUpdateArgs);
					if (layerUpdateArgs.effectsVectorLayer)
					{
						_mapVisualizer.UnregisterTilesFrom(VectorData.Factory);
						VectorData.UpdateFactorySettings();
						_mapVisualizer.ReregisterTilesTo(VectorData.Factory);
					}
					OnMapRedrawn();
				}
			};

			_terrain.UpdateLayer += (object sender, System.EventArgs eventArgs) =>
			{
				LayerUpdateArgs layerUpdateArgs = eventArgs as LayerUpdateArgs;
				if (layerUpdateArgs != null)
				{
					Debug.Log("<color=green>Terrain</color>");
					_mapVisualizer.UpdateTileForProperty(layerUpdateArgs.factory, layerUpdateArgs);
					if (layerUpdateArgs.effectsVectorLayer)
					{
						_mapVisualizer.UnregisterTilesFrom(VectorData.Factory);
						VectorData.UpdateFactorySettings();
						_mapVisualizer.ReregisterTilesTo(VectorData.Factory);
					}
					OnMapRedrawn();
				}
			};

			_vectorData.SubLayerRemoved += (object sender, EventArgs eventArgs) =>
			{
				VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

				if (layerUpdateArgs.visualizer != null)
				{
					_mapVisualizer.RemoveTilesFromLayer((VectorTileFactory)layerUpdateArgs.factory, layerUpdateArgs.visualizer);
				}

				Debug.Log("<color=blue>Vector</color>");
				OnMapRedrawn();
			};
			_vectorData.SubLayerAdded += (object sender, EventArgs eventArgs) =>
			{
				VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

				if (layerUpdateArgs.visualizer != null)
				{
					_mapVisualizer.UpdateTileForProperty(layerUpdateArgs.factory, layerUpdateArgs);
				}

				Debug.Log("<color=blue>Vector</color>");
				OnMapRedrawn();
			};
			_vectorData.UpdateLayer += (object sender, System.EventArgs eventArgs) =>
			{
				VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

				if (layerUpdateArgs.visualizer != null)
				{
					Debug.Log("UnregisterTiles");
					//we got a visualizer. Update only the visualizer.
					// No need to unload the entire factory to apply changes.
					_mapVisualizer.UnregisterTilesFromLayer((VectorTileFactory)layerUpdateArgs.factory, layerUpdateArgs.visualizer);
				}
				else
				{
					//We are updating a core property of vector section.
					//All vector features need to get unloaded and re-created.
					_mapVisualizer.UpdateTileForProperty(layerUpdateArgs.factory, layerUpdateArgs);
				}
				Debug.Log("<color=blue>Vector</color>");
				OnMapRedrawn();
			};

			_options.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				Debug.Log("<color=yellow>General </color>" + gameObject.name);
				//take care of redraw map business...

			};

			_options.locationOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				Debug.Log("<color=yellow>General - Location Options </color>" + gameObject.name);
				//take care of redraw map business...
				UpdateMap();
			};

			_options.extentOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				Debug.Log("<color=yellow>General - Extent Type Options </color>" + gameObject.name);
				//take care of redraw map business...
				OnTileProviderChanged();
			};

			_options.extentOptions.defaultExtents.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				Debug.Log("<color=yellow>General - Extent Options </color>" + gameObject.name);
				//take care of redraw map business...
				_tileProvider.UpdateTileExtent();
			};
			_options.placementOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				Debug.Log("<color=yellow>General - Placement Options </color>" + gameObject.name);
				//take care of redraw map business...
				SetPlacementStrategy();
				UpdateMap();
			};

			_options.scalingOptions.PropertyHasChanged += (object sender, System.EventArgs eventArgs) =>
			{
				Debug.Log("<color=yellow>General - Scaling Options </color>" + gameObject.name);
				//take care of redraw map business...
				SetScalingStrategy();
				UpdateMap();
			};

			_mapVisualizer.Initialize(this, _fileSource);
			_tileProvider.Initialize(this);

			SendInitialized();

			_tileProvider.UpdateTileExtent();
		}

		private void OnTileProviderChanged()
		{
			var currentTileProvider = gameObject.GetComponent<AbstractTileProvider>();

			if (currentTileProvider != null)
			{
				Destroy(currentTileProvider);
			}
			SetTileProvider();
			_tileProvider.Initialize(this);
			_tileProvider.UpdateTileExtent();
		}

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

		public virtual void UpdateMap()
		{
			UpdateMap(_centerLatitudeLongitude, Zoom);
		}

		public virtual void UpdateMap(Vector2d latLon)
		{
			UpdateMap(latLon, Zoom);
		}

		public virtual void UpdateMap(float zoom)
		{
			UpdateMap(_centerLatitudeLongitude, zoom);
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
			_tileProvider.UpdateTileExtent();

			if (OnUpdated != null)
			{
				OnUpdated();
			}
		}
		/// <summary>
		/// Resets the map.
		/// Use this method to reset the map to and reset all parameters.
		/// </summary>
		public void ResetMap()
		{
			Initialize(Conversions.StringToLatLon(_options.locationOptions.latitudeLongitude), (int)_options.locationOptions.zoom);
		}

		protected virtual void TileProvider_OnTileAdded(UnwrappedTileId tileId)
		{
			if (Options.placementOptions.snapMapToZero)
			{
				_worldHeightFixed = true;
				var tile = _mapVisualizer.LoadTile(tileId);
				if (tile.HeightDataState == MeshGeneration.Enums.TilePropertyState.Loaded)
				{
					var h = tile.QueryHeightData(.5f, .5f);
					Root.transform.position = new Vector3(
					 Root.transform.position.x,
					 -h,
					 Root.transform.position.z);
				}
				else
				{
					tile.OnHeightDataChanged += (s) =>
					{
						var h = s.QueryHeightData(.5f, .5f);
						Root.transform.position = new Vector3(
							 Root.transform.position.x,
							 -h,
							 Root.transform.position.z);
					};
				}
			}
			else
			{
				_mapVisualizer.LoadTile(tileId);
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

		#region Conversion Methods
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

		#region Location Prefabs Methods

		/// <summary>
		/// Places a prefab at the specified LatLon on the Map.
		/// </summary>
		/// <param name="prefab"> A Game Object Prefab.</param>
		/// <param name="LatLon">A Vector2d(Latitude Longitude) object</param>
		public void SpawnPrefabAtGeoLocation(GameObject prefab,
											 Vector2d LatLon,
											 Action<List<GameObject>> callback = null,
											 bool scaleDownWithWorld = true,
											 string locationItemName = "New Location")
		{
			var latLonArray = new Vector2d[] { LatLon };
			SpawnPrefabAtGeoLocation(prefab, latLonArray, callback, scaleDownWithWorld, locationItemName);
		}

		/// <summary>
		/// Places a prefab at all locations specified by the LatLon array.
		/// </summary>
		/// <param name="prefab"> A Game Object Prefab.</param>
		/// <param name="LatLon">A Vector2d(Latitude Longitude) object</param>
		public void SpawnPrefabAtGeoLocation(GameObject prefab,
											 Vector2d[] LatLon,
											 Action<List<GameObject>> callback = null,
											 bool scaleDownWithWorld = true,
											 string locationItemName = "New Location")
		{
			var coordinateArray = new string[LatLon.Length];
			for (int i = 0; i < LatLon.Length; i++)
			{
				coordinateArray[i] = LatLon[i].x + ", " + LatLon[i].y;
			}

			PrefabItemOptions item = new PrefabItemOptions()
			{
				findByType = LocationPrefabFindBy.AddressOrLatLon,
				prefabItemName = locationItemName,
				spawnPrefabOptions = new SpawnPrefabOptions()
				{
					prefab = prefab,
					scaleDownWithWorld = scaleDownWithWorld
				},

				coordinates = coordinateArray
			};

			if (callback != null)
			{
				item.OnAllPrefabsInstantiated += callback;
			}

			CreatePrefabLayer(item);
		}

		/// <summary>
		/// Places the prefab for supplied categories.
		/// </summary>
		/// <param name="prefab">GameObject Prefab</param>
		/// <param name="categories"><see cref="LocationPrefabCategories"/> For more than one category separate them by pipe
		/// (eg: LocationPrefabCategories.Food | LocationPrefabCategories.Nightlife)</param>
		/// <param name="density">Density controls the number of POIs on the map.(Integer value between 1 and 30)</param>
		/// <param name="locationItemName">Name of this location prefab item for future reference</param>
		/// <param name="scaleDownWithWorld">Should the prefab scale up/down along with the map game object?</param>
		public void SpawnPrefabByCategory(GameObject prefab,
										  LocationPrefabCategories categories = LocationPrefabCategories.AnyCategory,
										  int density = 30, Action<List<GameObject>> callback = null,
										  bool scaleDownWithWorld = true,
										  string locationItemName = "New Location")
		{
			PrefabItemOptions item = new PrefabItemOptions()
			{
				findByType = LocationPrefabFindBy.MapboxCategory,
				categories = categories,
				density = density,
				prefabItemName = locationItemName,
				spawnPrefabOptions = new SpawnPrefabOptions()
				{
					prefab = prefab,
					scaleDownWithWorld = scaleDownWithWorld
				}
			};

			if (callback != null)
			{
				item.OnAllPrefabsInstantiated += callback;
			}

			CreatePrefabLayer(item);
		}

		/// <summary>
		/// Places the prefab at POI locations if its name contains the supplied string
		/// <param name="prefab">GameObject Prefab</param>
		/// <param name="nameString">This is the string that will be checked against the POI name to see if is contained in it, and ony those POIs will be spawned</param>
		/// <param name="density">Density (Integer value between 1 and 30)</param>
		/// <param name="locationItemName">Name of this location prefab item for future reference</param>
		/// <param name="scaleDownWithWorld">Should the prefab scale up/down along with the map game object?</param>
		/// </summary>
		public void SpawnPrefabByName(GameObject prefab,
									  string nameString,
									  int density = 30,
									  Action<List<GameObject>> callback = null,
									  bool scaleDownWithWorld = true,
									  string locationItemName = "New Location")
		{
			PrefabItemOptions item = new PrefabItemOptions()
			{
				findByType = LocationPrefabFindBy.POIName,
				nameString = nameString,
				density = density,
				prefabItemName = locationItemName,
				spawnPrefabOptions = new SpawnPrefabOptions()
				{
					prefab = prefab,
					scaleDownWithWorld = scaleDownWithWorld
				}
			};

			CreatePrefabLayer(item);
		}

		/// <summary>
		/// Creates the prefab layer.
		/// </summary>
		/// <param name="item"> the options of the prefab layer.</param>
		private void CreatePrefabLayer(PrefabItemOptions item)
		{
			if (_vectorData.LayerProperty.sourceType == VectorSourceType.None
			|| !_vectorData.LayerProperty.sourceOptions.Id.Contains(MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreets).Id))
			{
				Debug.LogError("In order to place location prefabs please add \"mapbox.mapbox-streets-v7\" to the list of vector data sources");
				return;
			}

			//ensure that there is a vector layer
			if (_vectorData == null)
			{
				_vectorData = new VectorLayer();
			}

			_vectorData.AddLocationPrefabItem(item);
		}

		#endregion
	}
}
