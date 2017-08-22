namespace Mapbox.Unity.MeshGeneration.Factories
{
    using Mapbox.Platform;
    using Mapbox.Unity.MeshGeneration.Data;
    using System;
    using UnityEngine;

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

		internal virtual void Initialize(WorldProperties wp, IFileSource fileSource)
        {
			_progress = 0;
			_fileSource = fileSource;
            State = ModuleState.Initialized;
        }

        public void Register(UnityTile tile)
        {
            OnRegistered(tile);
        }

        public void Unregister(UnityTile tile)
        {
            OnUnregistered(tile);
        }

		internal virtual void PreInitialize(WorldProperties wp)
		{
			
		}

		internal abstract void OnRegistered(UnityTile tile);

        internal abstract void OnUnregistered(UnityTile tile);
    }
}