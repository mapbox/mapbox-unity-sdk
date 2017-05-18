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
	public class MapVisualization : ScriptableObject
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
				unityTile.Enable();
			}

			if (unityTile == null)
			{
				unityTile = new GameObject().AddComponent<UnityTile>();

				// TODO: don't parent if not editor. Instead, set localScale.
				unityTile.transform.SetParent(_map.Root, false);
			}

#if UNITY_EDITOR
			unityTile.gameObject.name = tileId.ToString();
#endif

			// TODO: simplify this.
			unityTile.Zoom = _map.Zoom;
			unityTile.RelativeScale = Conversions.GetTileScaleInMeters(0, _map.Zoom) / Conversions.GetTileScaleInMeters((float)_map.CenterLatitudeLongitude.x, _map.Zoom);
			unityTile.TileCoordinate = new Vector2(tileId.X, tileId.Y);
			unityTile.Rect = Conversions.TileBounds(unityTile.TileCoordinate, _map.Zoom);
			unityTile.transform.localPosition = new Vector3((float)(unityTile.Rect.Center.x - _map.CenterMercator.x), 0, (float)(unityTile.Rect.Center.y - _map.CenterMercator.y));

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

			unityTile.Disable();
			_activeTiles.Remove(tileId);
			_inactiveTiles.Enqueue(unityTile);
		}
	}
}