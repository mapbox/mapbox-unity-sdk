namespace Mapbox.Unity.MeshGeneration
{
	using Mapbox.Map;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Platform;
	using Utils;
	using Utilities;
	using Mapbox.Unity.Map;

	[CreateAssetMenu(menuName = "Mapbox/MapVisualization")]
	public class MapVisualizer : ScriptableObject
	{
		[SerializeField]
		AbstractTileFactory[] _factories;

		IMap _map;

		Dictionary<UnwrappedTileId, UnityTile> _activeTiles;
		Queue<UnityTile> _inactiveTiles;

		/// <summary>
		/// Initializes the factories by passing the file source down, which's necessary for data (web/file) calls
		/// </summary>
		/// <param name="fileSource"></param>
		public void Initialize(IMap map, IFileSource fileSource)
		{
			_map = map;
			_activeTiles = new Dictionary<UnwrappedTileId, UnityTile>();
			_inactiveTiles = new Queue<UnityTile>();

			foreach (var factory in _factories)
			{
				factory.Initialize(fileSource);
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
			foreach (var factory in _factories)
			{
				factory.Unregister(unityTile);
			}

			unityTile.Recycle();
			_activeTiles.Remove(tileId);
			_inactiveTiles.Enqueue(unityTile);
		}
	}
}