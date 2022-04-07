using Mapbox.Unity.DataContainers;
using Mapbox.Unity.Map.Interfaces;
using Mapbox.Unity.QuadTree;
using UnityEngine.UI;

namespace Mapbox.Unity.Map
{
	using System.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Platform;
	using UnityEngine.Serialization;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.MeshGeneration.Enums;
	using Mapbox.Unity.MeshGeneration.Interfaces;

	/// <summary>
	/// Map Visualizer
	/// Represents a map.Doesn’t contain much logic and at the moment, it creates requested tiles and relays them to the factories
	/// under itself.It has a caching mechanism to reuse tiles and does the tile positioning in unity world.
	/// Later we’ll most likely keep track of map features here as well to allow devs to query for features easier
	/// (i.e.query all buildings x meters around any restaurant etc).
	/// </summary>
	public abstract class AbstractMapVisualizer : ScriptableObject
	{
		public TerrainLayer TerrainLayer;
		public ImageryLayer ImageryLayer;
		public VectorLayer VectorLayer;

		protected IMapReadable _map;
		protected Dictionary<UnwrappedTileId, UnityTile> _activeTiles = new Dictionary<UnwrappedTileId, UnityTile>();
		protected ObjectPool<UnityTile> _tilePool;
		private int _counter;

		private ModuleState _state;
		public ModuleState State
		{
			get
			{
				return _state;
			}
			internal set
			{
				if (_state != value)
				{
					_state = value;
					OnMapVisualizerStateChanged(_state);
				}
			}
		}

		public IMapReadable Map { get { return _map; } }
		public Dictionary<UnwrappedTileId, UnityTile> ActiveTiles { get { return _activeTiles; } }
		public Dictionary<UnwrappedTileId, int> _tileProgress;

		public event Action<ModuleState> OnMapVisualizerStateChanged = delegate { };
		public event Action<UnityTile> OnTileFinished = delegate { };

		/// <summary>
		/// Initializes the factories by passing the file source down, which is necessary for data (web/file) calls
		/// </summary>
		/// <param name="fileSource"></param>
		public virtual void Initialize(IMapReadable map)
		{
			_map = map;
			_tileProgress = new Dictionary<UnwrappedTileId, int>();

			//Layers serialize so we are using Initialize method to pass parameters
			//on map start. Otherwise Layer object will not be null because of serialization
			//but everything not-serialized inside will be null.
			if (ImageryLayer == null)
			{
				ImageryLayer = new ImageryLayer();
			}
			ImageryLayer.FactoryError += Factory_OnTileError;
			ImageryLayer.OnEnabled += OnLayerOnEnabled;
			ImageryLayer.OnDisabled += OnLayerOnDisabled;
			ImageryLayer.Initialize();

			if (TerrainLayer == null)
			{
				TerrainLayer = new TerrainLayer();
			}
			TerrainLayer.FactoryError += Factory_OnTileError;
			TerrainLayer.OnEnabled += OnLayerOnEnabled;
			TerrainLayer.OnDisabled += OnLayerOnDisabled;
			TerrainLayer.Initialize();

			if (VectorLayer == null)
			{
				VectorLayer = new VectorLayer();
			}
			VectorLayer.FactoryError += Factory_OnTileError;
			VectorLayer.OnEnabled += OnLayerOnEnabled;
			VectorLayer.OnDisabled += OnLayerOnDisabled;
			VectorLayer.Initialize(() => _activeTiles.Values);

			// Allow for map re-use by recycling any active tiles.
			var activeTiles = _activeTiles.Keys.ToList();
			foreach (var tile in activeTiles)
			{
				DisposeTile(tile);
			}

			_tilePool = new ObjectPool<UnityTile>(() =>
			{
				var tile = new GameObject().AddComponent<UnityTile>();
				//tile.MeshRenderer.sharedMaterial = Instantiate(_map.TileMaterial);
				tile.MeshRenderer.material = _map.TileMaterial;
				tile.transform.SetParent(_map.Root, false);
				tile.gameObject.SetActive(false);
				if (TerrainLayer.Factory != null)
				{
					TerrainLayer.Factory.PregenerateTileMesh(tile);
				}

				tile.TileFinished += (t) =>
				{
					//t.gameObject.SetActive(true);
					OnTileFinished(t);
				};
				return tile;
			});

			State = ModuleState.Initialized;

			// foreach (var factory in Factories)
			// {
			// 	if (null == factory)
			// 	{
			// 		Debug.LogError("AbstractMapVisualizer: Factory is NULL");
			// 	}
			// 	else
			// 	{
			// 		factory.Initialize(fileSource);
			// 		UnregisterEvents(factory);
			// 		RegisterEvents(factory);
			// 	}
			// }



			//Set up events for changes.
			ImageryLayer.UpdateLayer += OnImageOrTerrainUpdateLayer;
			TerrainLayer.UpdateLayer += OnImageOrTerrainUpdateLayer;

			VectorLayer.SubLayerRemoved += OnVectorDataSubLayerRemoved;
			VectorLayer.SubLayerAdded += OnVectorDataSubLayerAdded;
			VectorLayer.UpdateLayer += OnVectorDataUpdateLayer;
		}

		private void OnLayerOnEnabled(AbstractLayer layer)
		{
			foreach (var tile in _activeTiles)
			{
				layer.Register(tile.Value);
			}
		}

		private void OnLayerOnDisabled(AbstractLayer layer)
		{
			foreach (var tile in _activeTiles)
			{
				layer.Unregister(tile.Value);
			}
		}

		private void OnImageOrTerrainUpdateLayer(object sender, System.EventArgs eventArgs)
		{
			LayerUpdateArgs layerUpdateArgs = eventArgs as LayerUpdateArgs;
			if (layerUpdateArgs != null)
			{
				UpdateTileForProperty(layerUpdateArgs.factory, layerUpdateArgs);
				if (layerUpdateArgs.effectsVectorLayer)
				{
					RedrawVectorDataLayer();
				}
			}
		}

		private void OnVectorDataSubLayerRemoved(object sender, EventArgs eventArgs)
		{
			VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

			if (layerUpdateArgs.visualizer != null)
			{
				RemoveTilesFromLayer((VectorTileFactory)layerUpdateArgs.factory, layerUpdateArgs.visualizer);
			}
		}

		private void OnVectorDataSubLayerAdded(object sender, EventArgs eventArgs)
		{
			RedrawVectorDataLayer();
		}

		private void OnVectorDataUpdateLayer(object sender, System.EventArgs eventArgs)
		{

			VectorLayerUpdateArgs layerUpdateArgs = eventArgs as VectorLayerUpdateArgs;

			if (layerUpdateArgs.visualizer != null)
			{
				//We have a visualizer. Update only the visualizer.
				//No need to unload the entire factory to apply changes.
				UnregisterAndRedrawTilesFromLayer((VectorTileFactory)layerUpdateArgs.factory, layerUpdateArgs.visualizer);
			}
			else
			{
				//We are updating a core property of vector section.
				//All vector features need to get unloaded and re-created.
				RedrawVectorDataLayer();
 			}
		}

		private void RedrawVectorDataLayer()
		{
			UnregisterTilesFrom(VectorLayer);
			// VectorLayer.UnbindAllEvents();
			// VectorLayer.UpdateFactorySettings();
			ReregisterTilesTo(VectorLayer);
		}

		public virtual void Destroy()
		{
			// if (Factories != null)
			// {
			// 	_counter = Factories.Count;
			// 	for (int i = 0; i < _counter; i++)
			// 	{
			// 		if (Factories[i] != null)
			// 		{
			// 			UnregisterEvents(Factories[i]);
			// 		}
			// 	}
			// }

			// Inform all downstream nodes that we no longer need to process these tiles.
			// This scriptable object may be re-used, but it's gameobjects are likely
			// to be destroyed by a scene change, for example.
			foreach (var tileId in _activeTiles.Keys.ToList())
			{
				DisposeTile(tileId);
			}

			_activeTiles.Clear();
			_tilePool?.Clear();
		}

		#region Factory event callbacks
		//factory event callback, not relaying this up for now

		// private void TileHeightStateChanged(UnityTile tile)
		// {
		// 	if (tile.HeightDataState == TilePropertyState.Loaded)
		// 	{
		// 		OnTileHeightProcessingFinished(tile);
		// 	}
		// 	TileStateChanged(tile);
		// }
		//
		// private void TileRasterStateChanged(UnityTile tile)
		// {
		// 	if (tile.RasterDataState == TilePropertyState.Loaded)
		// 	{
		// 		OnTileImageProcessingFinished(tile);
		// 	}
		// 	TileStateChanged(tile);
		// }
		//
		// private void TileVectorStateChanged(UnityTile tile)
		// {
		// 	if (tile.VectorDataState == TilePropertyState.Loaded)
		// 	{
		// 		OnTileVectorProcessingFinished(tile);
		// 	}
		// 	TileStateChanged(tile);
		// }

		public virtual void TileStateChanged(UnityTile tile)
		{
			// bool rasterDone = (tile.RasterDataState == TilePropertyState.None ||
			// 					tile.RasterDataState == TilePropertyState.Loaded ||
			// 					tile.RasterDataState == TilePropertyState.Error ||
			// 					tile.RasterDataState == TilePropertyState.Cancelled);
			//
			// bool terrainDone = (tile.HeightDataState == TilePropertyState.None ||
			// 					tile.HeightDataState == TilePropertyState.Loaded ||
			// 					 tile.HeightDataState == TilePropertyState.Error ||
			// 					 tile.HeightDataState == TilePropertyState.Cancelled);
			// bool vectorDone = (tile.VectorDataState == TilePropertyState.None ||
			// 					tile.VectorDataState == TilePropertyState.Loaded ||
			// 					tile.VectorDataState == TilePropertyState.Error ||
			// 					tile.VectorDataState == TilePropertyState.Cancelled);

			// if (rasterDone && terrainDone && vectorDone)
			// {
			// 	tile.gameObject.SetActive(true);
			// 	tile.TileState = MeshGeneration.Enums.TilePropertyState.Loaded;
			// 	OnTileFinished(tile);
			//
			// 	// Check if all tiles in extent are active tiles
			// 	if (_map.CurrentExtent.Count == _activeTiles.Count)
			// 	{
			// 		bool allDone = true;
			// 		// Check if all tiles are loaded.
			// 		foreach (var currentTile in _map.CurrentExtent)
			// 		{
			// 			allDone = allDone && (_activeTiles.ContainsKey(currentTile) && _activeTiles[currentTile].TileState == TilePropertyState.Loaded);
			// 		}
			//
			// 		if (allDone)
			// 		{
			// 			State = ModuleState.Finished;
			// 		}
			// 		else
			// 		{
			// 			State = ModuleState.Working;
			// 		}
			// 	}
			// 	else
			// 	{
			// 		State = ModuleState.Working;
			// 	}
			//
			//
			// }
		}
		#endregion

		public virtual UnityTile LoadTile(NewTileParameters parameters)
		{
			var tileId = parameters.TileId;
			var enableTile = parameters.InitializeVisible;

			if (ActiveTiles.ContainsKey(parameters.TileId))
				return null;

			var unityTile = _tilePool.GetObject();
			var tileTransform = unityTile.transform;
			unityTile.gameObject.SetActive(enableTile);
			unityTile.Initialize(_map, tileId, TerrainLayer.IsLayerActive && TerrainLayer.ElevationType != ElevationLayerType.FlatTerrain);
			tileTransform.position = parameters.UnityRectD.Center;
			tileTransform.SetParent(_map.Root);
			tileTransform.localScale = Vector3.one * parameters.UnityRectD.UnityBounds.size.x;

			//PlaceTile(tileId, unityTile, _map);

			// Don't spend resources naming objects, as you shouldn't find objects by name anyway!
#if UNITY_EDITOR
			unityTile.gameObject.name = unityTile.CanonicalTileId.ToString();
#endif

			ActiveTiles.Add(tileId, unityTile);

			if (ImageryLayer.IsLayerActive)
			{
				ImageryLayer.Register(unityTile, enableTile);
			}

			if (TerrainLayer.IsLayerActive)
			{
				TerrainLayer.Register(unityTile, enableTile);
			}

			if (VectorLayer.IsLayerActive)
			{
				VectorLayer.Register(unityTile);
			}

			unityTile.SetFinishCondition();


			if (enableTile)
			{
				unityTile.gameObject.SetActive(true);
			}

			return unityTile;
		}

		/// <summary>
		/// Registers requested tiles to the factories
		/// </summary>
		/// <param name="tileId"></param>
		public virtual UnityTile LoadTile(UnwrappedTileId tileId, bool enableTile = false)
		{
			if (ActiveTiles.ContainsKey(tileId))
				return null;

			var unityTile = _tilePool.GetObject();
			unityTile.gameObject.SetActive(enableTile);
			unityTile.Initialize(_map, tileId, TerrainLayer.IsLayerActive && TerrainLayer.ElevationType != ElevationLayerType.FlatTerrain);

			PlaceTile(tileId, unityTile, _map);

			// Don't spend resources naming objects, as you shouldn't find objects by name anyway!
#if UNITY_EDITOR
			unityTile.gameObject.name = unityTile.CanonicalTileId.ToString();
#endif

			ActiveTiles.Add(tileId, unityTile);

			if (ImageryLayer.IsLayerActive)
			{
				ImageryLayer.Register(unityTile, enableTile);
			}

			if (TerrainLayer.IsLayerActive)
			{
				TerrainLayer.Register(unityTile, enableTile);
			}

			if (VectorLayer.IsLayerActive)
			{
				VectorLayer.Register(unityTile);
			}

			unityTile.SetFinishCondition();


			if (enableTile)
			{
				unityTile.gameObject.SetActive(true);
			}

			return unityTile;
		}

		public virtual void StopTile(UnwrappedTileId tileId)
		{
			if (_activeTiles.ContainsKey(tileId))
			{
				var unityTile = _activeTiles[tileId];
				StopTile(unityTile);
				unityTile.Logs.Add(string.Format("{0} - {1}", Time.frameCount, "Stopped"));
			}
		}

		public virtual void StopTile(UnityTile unityTile)
		{
			if (unityTile != null && unityTile.Tiles.Count > 0)
			{
				// foreach (var factory in Factories)
				// {
				// 	factory.Unregister(unityTile);
				// }

				unityTile.IsStopped = true;
				if (ImageryLayer.IsLayerActive)
				{
					ImageryLayer.Unregister(unityTile);
				}

				if (TerrainLayer.IsLayerActive)
				{
					TerrainLayer.Unregister(unityTile);
				}

				if (VectorLayer.IsLayerActive)
				{
					VectorLayer.Unregister(unityTile);
				}

			}
		}

		public virtual void DisposeTile(UnwrappedTileId tileId)
		{
			if (!ActiveTiles.ContainsKey(tileId))
			{
				return;
			}

			var unityTile = ActiveTiles[tileId];
			unityTile.Logs
				.Add(string.Format("{0} - {1}", Time.frameCount, "DisposeTile"));

			if (unityTile != null)
			{
				MapboxAccess.Instance.TaskManager.CancelTile(unityTile.CanonicalTileId);
				OnTileDisposing(unityTile);

				// foreach (var factory in Factories)
				// {
				// 	factory.Unregister(unityTile);
				// }

				if (!unityTile.IsStopped)
				{
					if (ImageryLayer.IsLayerActive)
					{
						ImageryLayer.Unregister(unityTile);
					}

					if (TerrainLayer.IsLayerActive)
					{
						TerrainLayer.Unregister(unityTile);
					}

					if (VectorLayer.IsLayerActive)
					{
						VectorLayer.Unregister(unityTile);
					}
				}
				ImageryLayer.ClearTile(unityTile);
				TerrainLayer.ClearTile(unityTile);
				VectorLayer.ClearTile(unityTile);

				unityTile.Recycle();
				ActiveTiles.Remove(tileId);
				_tilePool.Put(unityTile);
			}
		}

		/// <summary>
		/// Repositions active tiles instead of recreating them. Useful for panning the map
		/// </summary>
		/// <param name="tileId"></param>
		public virtual void RepositionTile(UnwrappedTileId tileId)
		{
			UnityTile currentTile;
			if (ActiveTiles.TryGetValue(tileId, out currentTile))
			{
				PlaceTile(tileId, currentTile, _map);
			}
		}

		protected abstract void PlaceTile(UnwrappedTileId tileId, UnityTile tile, IMapReadable map);

		public void ClearMap()
		{
			UnregisterAllTiles();
			TerrainLayer.Clear();
			ImageryLayer.Clear();
			VectorLayer.Clear();
			foreach (var tileId in _activeTiles.Keys.ToList())
			{
				_activeTiles[tileId].ClearAssets();
				DisposeTile(tileId);
			}

			if (_tilePool != null)
			{
				foreach (var tile in _tilePool.GetQueue())
				{
					tile.ClearAssets();
					DestroyImmediate(tile.gameObject);
				}
				_tilePool.Clear();
			}

			State = ModuleState.Initialized;
		}

		public void ReregisterAllTiles()
		{
			foreach (var activeTile in _activeTiles)
			{
				TerrainLayer.Register(activeTile.Value);
				ImageryLayer.Register(activeTile.Value);
				VectorLayer.Register(activeTile.Value);
			}
		}

		public void UnregisterAllTiles()
		{
			var tileIds = _activeTiles.Select(x => x.Key).ToList();
			foreach (var tileId in tileIds)
			{
				DisposeTile(tileId);
			}
		}

		public void UnregisterTilesFrom(AbstractLayer layer)
		{
			if (VectorLayer.IsLayerActive)
			{
				foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
				{
					layer.Unregister(tileBundle.Value);
				}
			}
		}

		public void UnregisterAndRedrawTilesFromLayer(VectorTileFactory factory, LayerVisualizerBase layerVisualizer)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.UnregisterLayer(tileBundle.Value, layerVisualizer);
			}
			layerVisualizer.Clear();
			layerVisualizer.UnbindSubLayerEvents();
			layerVisualizer.SetProperties(layerVisualizer.SubLayerProperties);
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.RedrawSubLayer(tileBundle.Value, layerVisualizer);
			}
		}

		public void RemoveTilesFromLayer(VectorTileFactory factory, LayerVisualizerBase layerVisualizer)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.UnregisterLayer(tileBundle.Value, layerVisualizer);
			}
			factory.RemoveVectorLayerVisualizer(layerVisualizer);
		}

		public void ReregisterTilesTo(AbstractLayer layer)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				layer.Register(tileBundle.Value);
			}
		}

		public void UpdateTileForProperty(AbstractTileFactory factory, LayerUpdateArgs updateArgs)
		{
			foreach (KeyValuePair<UnwrappedTileId, UnityTile> tileBundle in _activeTiles)
			{
				factory.UpdateTileProperty(tileBundle.Value, updateArgs);
			}
		}

		#region Events
		/// <summary>
		/// The  <c>OnTileError</c> event triggers when there's a <c>Tile</c> error.
		/// Returns a <see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance as a parameter, for the tile on which error occurred.
		/// </summary>
		public event EventHandler<TileErrorEventArgs> OnTileError;
		private void Factory_OnTileError(object sender, TileErrorEventArgs e)
		{
			EventHandler<TileErrorEventArgs> handler = OnTileError;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		public event Action<UnityTile> OnTileDisposing = delegate { };

		#endregion

		public Queue<UnityTile> GetInactiveTiles => _tilePool?.GetQueue() as Queue<UnityTile>;
	}
}
