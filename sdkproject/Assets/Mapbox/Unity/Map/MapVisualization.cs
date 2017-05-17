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

		/// <summary>
		/// Initializes the factories by passing the file source down, which's necessary for data (web/file) calls
		/// </summary>
		/// <param name="fileSource"></param>
		public void Initialize(IMap map, IFileSource fileSource)
		{
			_map = map;
			_activeTiles = new Dictionary<UnwrappedTileId, UnityTile>();

			foreach (var factory in _factories)
			{
				factory.Initialize(fileSource);
			}
		}

		/// <summary>
		/// Registers requested tiles to the factories
		/// </summary>
		/// <param name="tile"></param>
		public void InitializeTile(UnwrappedTileId tileId)
		{
			// TODO delay any creation. Let factories handle this gameobject instantiation and such?
			var tile = new GameObject(tileId.ToString()).AddComponent<UnityTile>();
			tile.Zoom = _map.Zoom;
			tile.RelativeScale = Conversions.GetTileScaleInMeters(0, _map.Zoom) / Conversions.GetTileScaleInMeters((float)_map.CenterLatitudeLongitude.x, _map.Zoom);
			tile.TileCoordinate = new Vector2(tileId.X, tileId.Y);
			tile.Rect = Conversions.TileBounds(tile.TileCoordinate, _map.Zoom);
			tile.transform.SetParent(_map.Root, false);
			tile.transform.localPosition = new Vector3((float)(tile.Rect.Center.x - _map.CenterMercator.x), 0, (float)(tile.Rect.Center.y - _map.CenterMercator.y));

			_activeTiles.Add(tileId, tile);

			foreach (var factory in _factories)
			{
				factory.Register(tile);
			}
		}

		public void DisposeTile(UnwrappedTileId tileId)
		{
			var unityTile = _activeTiles[tileId];
			foreach (var factory in _factories)
			{
				factory.Unregister(unityTile);
			}

			// TODO; destroy or recycle objects in factories, instead.
			_activeTiles.Remove(tileId);

			// TODO: recycle!
			// FIXME: at some point game object is destroyed but should be visible? Race condition?
			Destroy(unityTile.gameObject);

		}
	}
}