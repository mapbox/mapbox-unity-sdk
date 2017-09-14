namespace Mapbox.Unity.Examples.DynamicZoom
{
	using Mapbox.Map;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.MeshGeneration.Factories;
	using Mapbox.Platform;
	using Mapbox.Unity.Map;
	using System;
	using Mapbox.Unity.MeshGeneration;
	using Mapbox.Utils;
	using Mapbox.Unity.Utilities;

	[CreateAssetMenu(menuName = "Mapbox/DynamicZoomMapVisualizer")]
	public class DynamicZoomMapVisualizer : AbstractMapVisualizer
	{



		/// <summary>
		/// Registers requested tiles to the factories
		/// </summary>
		/// <param name="tileId"></param>
		public override UnityTile LoadTile(UnwrappedTileId tileId)
		{
			UnityTile unityTile = null;

			if (InactiveTiles.Count > 0)
			{
				unityTile = InactiveTiles.Dequeue();
			}

			if (unityTile == null)
			{
				unityTile = new GameObject().AddComponent<UnityTile>();
				unityTile.transform.SetParent(_map.Root, false);
				if (null == unityTile.MeshRenderer)
				{
					MeshRenderer mr = unityTile.gameObject.AddComponent<MeshRenderer>();
					//avoid pink tiles on first use
					mr.enabled = false;
				}
				if (unityTile.MeshFilter == null) { unityTile.gameObject.AddComponent<MeshFilter>(); }
			}

			DynamicZoomMap map = _map as DynamicZoomMap;
			//HACK: switch COORDINATES - there's a bug somewhere with switched x<->y
			Vector2d centerWebMercDUMMY = new Vector2d(map.CenterWebMerc.y, map.CenterWebMerc.x);
			UnwrappedTileId centerTile = TileCover.WebMercatorToTileId(centerWebMercDUMMY, _map.Zoom);
			Vector2d centerTileCenter = Conversions.TileIdToCenterWebMercator(centerTile.X, centerTile.Y, _map.Zoom);
			Vector2d shift = map.CenterWebMerc - centerTileCenter;
			//float factor = Conversions.GetTileScaleInMeters((float)map.CenterLatitudeLongitude.x, map.Zoom) * 256 / ((DynamicZoomMap)_map).UnityTileSize;
			int unityTileSize = map.UnityTileSize;
			// get factor at equator to avoid shifting errors at higher latitudes
			float factor = Conversions.GetTileScaleInMeters(0f, _map.Zoom) * 256 / unityTileSize;

			Vector3 unityTileScale = new Vector3(unityTileSize, 1, unityTileSize);

			//position the tile relative to the center tile of the current viewport using the tile id
			//multiply by tile size Unity units (unityTileScale)
			//shift by distance of current viewport center to center of center tile
			Vector3 position = new Vector3(
				(tileId.X - centerTile.X) * unityTileSize - (float)shift.x / factor
				, 0
				, (centerTile.Y - tileId.Y) * unityTileSize - (float)shift.y / factor
			);


			//unityTile.Initialize(_map, tileId, 1f / factor/*, position*/);
			unityTile.Initialize(_map, tileId);
			unityTile.transform.localPosition = position;
			unityTile.transform.localScale = unityTileScale;

			foreach (var factory in Factories)
			{
				factory.Register(unityTile);
			}

			Tiles.Add(tileId, unityTile);

			return unityTile;
		}



	}
}