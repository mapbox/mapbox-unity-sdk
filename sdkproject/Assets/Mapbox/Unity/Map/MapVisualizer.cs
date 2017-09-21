namespace Mapbox.Unity.MeshGeneration
{
	using Mapbox.Map;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Platform;
	using Mapbox.Unity.Map;
	using System;
	using Mapbox;

	public enum ModuleState
	{
		Initialized,
		Working,
		Finished
	}

	public class AssignmentTypeAttribute : PropertyAttribute
	{
		public System.Type Type;

		public AssignmentTypeAttribute(System.Type t)
		{
			Type = t;
		}
	}

	[CreateAssetMenu(menuName = "Mapbox/MapVisualizer")]
	public class MapVisualizer : ScriptableObject, IMapVisualizer
	{
		[SerializeField]
		[NodeEditorElementAttribute("Factories")]
		private List<AbstractTileFactory> _factories;
		public List<AbstractTileFactory> Factories { get { return _factories; } }

		private IMap _map;
		public IMap Map { get { return _map; } }


		private Dictionary<UnwrappedTileId, UnityTile> _tiles;
		public Dictionary<UnwrappedTileId, UnityTile> Tiles { get { return _tiles; } }


		private Queue<UnityTile> _inactiveTiles;
		public Queue<UnityTile> InactiveTiles { get { return _inactiveTiles; } }



		private ModuleState _state;
		public ModuleState State
		{
			get
			{
				return _state;
			}
			private set
			{
				if (_state != value)
				{
					_state = value;
					OnMapVisualizerStateChanged(_state);
				}
			}
		}

		public event Action<ModuleState> OnMapVisualizerStateChanged = delegate { };

		/// <summary>
		/// Initializes the factories by passing the file source down, which's necessary for data (web/file) calls
		/// </summary>
		/// <param name="fileSource"></param>
		public void Initialize(IMap map, IFileSource fileSource)
		{
			_map = map;
			_tiles = new Dictionary<UnwrappedTileId, UnityTile>();
			_inactiveTiles = new Queue<UnityTile>();
			State = ModuleState.Initialized;

			foreach (var factory in _factories)
			{
				factory.Initialize(fileSource);
				factory.OnFactoryStateChanged += UpdateState;
			}
		}

		private void UpdateState(AbstractTileFactory factory)
		{
			if (State != ModuleState.Working && factory.State == ModuleState.Working)
			{
				State = ModuleState.Working;
			}
			else if (State != ModuleState.Finished && factory.State == ModuleState.Finished)
			{
				var allFinished = true;
				for (int i = 0; i < _factories.Count; i++)
				{
					if (_factories[i] != null)
					{
						allFinished &= _factories[i].State == ModuleState.Finished;
					}
				}
				if (allFinished)
				{
					State = ModuleState.Finished;
				}
			}
		}

		public void Destroy()
		{
			for (int i = 0; i < _factories.Count; i++)
			{
				if (_factories[i] != null)
					_factories[i].OnFactoryStateChanged -= UpdateState;
			}
		}

		/// <summary>
		/// Registers requested tiles to the factories
		/// </summary>
		/// <param name="tileId"></param>
		public UnityTile LoadTile(UnwrappedTileId tileId)
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

			unityTile.Initialize(_map, tileId, _map.WorldRelativeScale);

			foreach (var factory in _factories)
			{
				if (factory != null)
					factory.Register(unityTile);
			}

			_tiles.Add(tileId, unityTile);

			return unityTile;
		}

		public void DisposeTile(UnwrappedTileId tileId)
		{
			var unityTile = _tiles[tileId];

			unityTile.Recycle();
			_tiles.Remove(tileId);
			_inactiveTiles.Enqueue(unityTile);

			foreach (var factory in _factories)
			{
				factory.Unregister(unityTile);
			}
		}
	}
}