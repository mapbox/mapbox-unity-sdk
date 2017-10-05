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

	public abstract class AbstractMapVisualizer : ScriptableObject
	{
		[SerializeField]
		[NodeEditorElementAttribute("Factories")]
		[FormerlySerializedAs("_factories")]
		public List<AbstractTileFactory> Factories;

		[SerializeField]
		Texture2D _loadingTexture;

		protected IMapReadable _map;
		protected Dictionary<UnwrappedTileId, UnityTile> _activeTiles = new Dictionary<UnwrappedTileId, UnityTile>();
		protected Queue<UnityTile> _inactiveTiles = new Queue<UnityTile>();

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

		public event Action<ModuleState> OnMapVisualizerStateChanged = delegate { };

		/// <summary>
		/// Initializes the factories by passing the file source down, which is necessary for data (web/file) calls
		/// </summary>
		/// <param name="fileSource"></param>
		public void Initialize(IMapReadable map, IFileSource fileSource)
		{
			_map = map;

			// Allow for map re-use by recycling any active tiles.
			var activeTiles = _activeTiles.Keys.ToList();
			foreach (var tile in activeTiles)
			{
				DisposeTile(tile);
			}

			State = ModuleState.Initialized;

			foreach (var factory in Factories)
			{
				if (null == factory)
				{
					Debug.LogError("AbstractMapVisualizer: Factory is NULL");
				}
				else
				{
					factory.Initialize(fileSource);
					factory.OnFactoryStateChanged += UpdateState;
				}
			}
		}

		public void Destroy()
		{
			for (int i = 0; i < Factories.Count; i++)
			{
				if (Factories[i] != null)
				{
					Factories[i].OnFactoryStateChanged -= UpdateState;
				}
			}

			// Cleanup gameobjects and clear lists!
			// This scriptable object may be re-used, but it's gameobjects are likely 
			// to be destroyed by a scene change, for example. 
			foreach (var tile in _activeTiles.Values.ToList())
			{
				Destroy(tile.gameObject);
			}

			foreach (var tile in _inactiveTiles)
			{
                Destroy(tile.gameObject);
			}

			_activeTiles.Clear();
			_inactiveTiles.Clear();
		}

		internal void UpdateState(AbstractTileFactory factory)
		{
			if (State != ModuleState.Working && factory.State == ModuleState.Working)
			{
				State = ModuleState.Working;
			}
			else if (State != ModuleState.Finished && factory.State == ModuleState.Finished)
			{
				var allFinished = true;
				for (int i = 0; i < Factories.Count; i++)
				{
					if (Factories[i] != null)
					{
						allFinished &= Factories[i].State == ModuleState.Finished;
					}
				}
				if (allFinished)
				{
					State = ModuleState.Finished;
				}
			}
		}

		/// <summary>
		/// Registers requested tiles to the factories
		/// </summary>
		/// <param name="tileId"></param>
		public virtual UnityTile LoadTile(UnwrappedTileId tileId)
		{
			UnityTile unityTile = null;

			if (_inactiveTiles.Count > 0)
			{
				unityTile = _inactiveTiles.Dequeue();
			}

			if (unityTile == null)
			{
				unityTile = new GameObject().AddComponent<UnityTile>();
				unityTile.transform.SetParent(_map.Root, false);
			}

			unityTile.Initialize(_map, tileId, _map.WorldRelativeScale, _loadingTexture);
			PlaceTile(tileId, unityTile, _map);

#if UNITY_EDITOR
			unityTile.gameObject.name = unityTile.CanonicalTileId.ToString();
#endif

			foreach (var factory in Factories)
			{
				factory.Register(unityTile);
			}

			ActiveTiles.Add(tileId, unityTile);

			return unityTile;
		}

		public void DisposeTile(UnwrappedTileId tileId)
		{
			var unityTile = ActiveTiles[tileId];

			unityTile.Recycle();
			ActiveTiles.Remove(tileId);
			_inactiveTiles.Enqueue(unityTile);

			foreach (var factory in Factories)
			{
				factory.Unregister(unityTile);
			}
		}

		protected abstract void PlaceTile(UnwrappedTileId tileId, UnityTile tile, IMapReadable map);
	}
}