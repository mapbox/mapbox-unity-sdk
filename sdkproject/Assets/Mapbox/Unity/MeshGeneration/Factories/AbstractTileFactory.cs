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
        /// The OnTileError event triggers when there's tile error.
        /// It returns the Mapbox.Map.CanonicalTileId instance for the tile on which error occurred.
        /// </summary>
        public event Action<CanonicalTileId> OnTileError = delegate { };

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
            tile.OnTileErrorEvent += Tile_OnTileErrorEvent;
        }

        public void Unregister(UnityTile tile)
        {
            OnUnregistered(tile);
            tile.OnTileErrorEvent -= Tile_OnTileErrorEvent;
        }

        private void Tile_OnTileErrorEvent(CanonicalTileId id)
        {
            if (OnTileError != null)
            {
                OnTileError(id);
            }
        }

        internal abstract void OnInitialized();

        internal abstract void OnRegistered(UnityTile tile);

        internal abstract void OnUnregistered(UnityTile tile);
    }
}