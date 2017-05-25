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

	public enum ModuleState
	{
		Initialized,
		Working,
		Finished
	}

	[CreateAssetMenu(menuName = "Mapbox/MapVisualization")]
	public class MapVisualizer : ScriptableObject
	{
		[SerializeField]
		AbstractTileFactory[] _factories;

		IMap _map;
		Dictionary<UnwrappedTileId, UnityTile> _activeTiles;
		Queue<UnityTile> _inactiveTiles;

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
			_activeTiles = new Dictionary<UnwrappedTileId, UnityTile>();
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
				for (int i = 0; i < _factories.Length; i++)
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

		internal void Destroy()
		{
			for (int i = 0; i < _factories.Length; i++)
			{
				if (_factories[i] != null)
					_factories[i].OnFactoryStateChanged -= UpdateState;
			}
		}

		/// <summary>
		/// Registers requested tiles to the factories
		/// </summary>
		/// <param name="tileId"></param>
		public void LoadTile(UnwrappedTileId tileId)
		{
			UnityTile unityTile = null;

			if (_inactiveTiles.Count > 0)
			{
				unityTile = _inactiveTiles.Dequeue();
			}

			if (unityTile == null)
			{
				unityTile = new GameObject().AddComponent<UnityTile>();

#if !UNITY_EDITOR
				unityTile.transform.localScale = Unity.Constants.Math.Vector3One * _map.WorldRelativeScale;
#else
				unityTile.transform.SetParent(_map.Root, false);
#endif
			}

			unityTile.Initialize(_map, tileId);

			foreach (var factory in _factories)
			{
				factory.Register(unityTile);
			}

			_activeTiles.Add(tileId, unityTile);
		}

		public void DisposeTile(UnwrappedTileId tileId)
		{
			var unityTile = _activeTiles[tileId];

			unityTile.Recycle();
			_activeTiles.Remove(tileId);
			_inactiveTiles.Enqueue(unityTile);

			foreach (var factory in _factories)
			{
				factory.Unregister(unityTile);
			}
		}
	}
}