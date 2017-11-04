namespace Mapbox.Unity.MeshGeneration.Factories
{
	using Mapbox.Platform;
	using Mapbox.Unity.MeshGeneration.Data;
	using System;
	using UnityEngine;
	using Mapbox.Unity.Map;
	using Mapbox.Map;

public abstract class AbstractTileFactory : ScriptableObject
	{
		protected IFileSource _fileSource;

		public ModuleState State { get; private set; }

		private int _progress;
		protected int Progress
		{
			get
			{
				return _progress;
			}
			set
			{
				if (_progress == 0 && value > 0)
				{
					State = ModuleState.Working;
					OnFactoryStateChanged(this);
				}
				if (_progress > 0 && value == 0)
				{
					State = ModuleState.Finished;
					OnFactoryStateChanged(this);
				}
				_progress = value;
			}
		}

		public event Action<AbstractTileFactory> OnFactoryStateChanged = delegate { };
		/// <summary>
		/// The  <c>OnTileError</c> event triggers when there's <c>Tile</c> error.
		/// Returns a <see cref="T:Mapbox.Map.TileErrorEventArgs"/> instance as a parameter, for the tile on which error occurred.
		/// </summary>
		public event EventHandler<TileErrorEventArgs> OnTileError;

		public void Initialize(IFileSource fileSource)
		{
			_progress = 0;
			_fileSource = fileSource;
			State = ModuleState.Initialized;
			OnInitialized();
		}

		public void Register(UnityTile tile)
		{
			OnRegistered(tile);
			tile.OnTileError += Tile_OnTileErrorEvent;
		}

		public void Unregister(UnityTile tile)
		{
			OnUnregistered(tile);
			tile.OnTileError -= Tile_OnTileErrorEvent;
		}

		private void Tile_OnTileErrorEvent(object sender, TileErrorEventArgs e)
		{
			EventHandler<TileErrorEventArgs> handler = OnTileError;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		internal abstract void OnInitialized();

		internal abstract void OnRegistered(UnityTile tile);

		internal abstract void OnUnregistered(UnityTile tile);
	}
}