namespace Mapbox.Unity.MeshGeneration.Factories
{
	using Mapbox.Platform;
	using Mapbox.Unity.MeshGeneration.Data;
	using UnityEngine;

	public abstract class AbstractTileFactory : ScriptableObject
	{
		protected IFileSource _fileSource;

		public void Initialize(IFileSource fileSource)
		{
			_fileSource = fileSource;
			OnInitialized();
		}

		public void Register(UnityTile tile)
		{
			OnRegistered(tile);
		}

		public void Unregister(UnityTile tile)
		{
			tile.Cancel();
			OnUnregistered(tile);
		}

		internal abstract void OnInitialized();

		internal abstract void OnRegistered(UnityTile tile);

		internal abstract void OnUnregistered(UnityTile tile);
	}
}