namespace Mapbox.Unity.MeshGeneration
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using Mapbox.Unity.Map;
	using Mapbox.Platform;

	public abstract class AbstractMapVisualizer : ScriptableObject, IMapVisualizer
	{
		public List<AbstractTileFactory> Factories { get { return _factories; } }

		[SerializeField]
		private List<AbstractTileFactory> _factories;

		internal IMap _map;
		public IMap Map { get { return _map; } internal set { _map = value; } }

		public Queue<UnityTile> InactiveTiles { get; set; }


		public Dictionary<UnwrappedTileId, UnityTile> Tiles { get; set; }

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

		public event Action<ModuleState> OnMapVisualizerStateChanged = delegate { };

		/// <summary>
		/// Initializes the factories by passing the file source down, which's necessary for data (web/file) calls
		/// </summary>
		/// <param name="fileSource"></param>
		public void Initialize(IMap map, IFileSource fileSource)
		{
			Map = map;
			Tiles = new Dictionary<UnwrappedTileId, UnityTile>();
			InactiveTiles = new Queue<UnityTile>();
			State = ModuleState.Initialized;

			foreach (var factory in Factories)
			{
				factory.Initialize(fileSource);
				factory.OnFactoryStateChanged += UpdateState;
			}
		}

		public void Destroy()
		{
			for (int i = 0; i < Factories.Count; i++)
			{
				if (Factories[i] != null)
					Factories[i].OnFactoryStateChanged -= UpdateState;
			}
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

			if (InactiveTiles.Count > 0)
			{
				unityTile = InactiveTiles.Dequeue();
			}

			if (unityTile == null)
			{
				unityTile = new GameObject().AddComponent<UnityTile>();
				unityTile.transform.SetParent(Map.Root, false);
			}

			//unityTile.Initialize(_map, tileId, _map.WorldRelativeScale);
			unityTile.Initialize(Map, tileId, _map.WorldRelativeScale);

			foreach (var factory in Factories)
			{
				factory.Register(unityTile);
			}

			Tiles.Add(tileId, unityTile);

			return unityTile;
		}

		public void DisposeTile(UnwrappedTileId tileId)
		{
			var unityTile = Tiles[tileId];

			unityTile.Recycle();
			Tiles.Remove(tileId);
			InactiveTiles.Enqueue(unityTile);

			foreach (var factory in Factories)
			{
				factory.Unregister(unityTile);
			}
		}
	}
}