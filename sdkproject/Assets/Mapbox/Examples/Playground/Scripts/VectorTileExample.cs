//-----------------------------------------------------------------------
// <copyright file="VectorTileExample.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Mapbox.Examples.Playground
{
	using Mapbox.Unity;
	using Mapbox.Platform;
	using System;
	using UnityEngine;
	using UnityEngine.UI;
	using Mapbox.Map;
	using Mapbox.Json;
	using Mapbox.VectorTile.ExtensionMethods;
	using Mapbox.Utils.JsonConverters;
	using Mapbox.Geocoding;

	public class VectorTileExample : MonoBehaviour, Mapbox.Utils.IObserver<VectorTile>
	{
		[SerializeField]
		ForwardGeocodeUserInput _searchLocation;

		[SerializeField]
		Text _resultsText;

		Map<VectorTile> _map;

		void Awake()
		{
			_searchLocation.OnGeocoderResponse += SearchLocation_OnGeocoderResponse;
		}

		void OnDestroy()
		{
			if (_searchLocation != null)
			{
				_searchLocation.OnGeocoderResponse -= SearchLocation_OnGeocoderResponse;
			}
		}

		void Start()
		{
			_map = new Map<VectorTile>(new FileSource(MapboxAccess.Instance.Configuration.AccessToken));
			_map.Zoom = 18;
			// This marks us an an observer to map.
			// We will get each tile in OnNext(VectorTile tile) as they become available.
			_map.Subscribe(this);
			_map.Update();
		}

		/// <summary>
		/// Search location was changed.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		void SearchLocation_OnGeocoderResponse(ForwardGeocodeResponse response)
		{
			Redraw();
		}

		/// <summary>
		/// Request _map to update its tile data with new coordinates.
		/// </summary>
		void Redraw()
		{
			if (!_searchLocation.HasResponse)
			{
				_resultsText.text = "no results";
				return;
			}

			//zoom in to get results for consecutive searches
			_map.Center = _searchLocation.Coordinate;
			_map.Update();
		}

		/// <summary>
		/// Handle tile data from _map as they become available.
		/// </summary>
		/// <param name="tile">Tile.</param>
		public void OnNext(VectorTile tile)
		{
			if (tile.CurrentState != Tile.State.Loaded || tile.HasError)
			{
				return;
			}

			var data = JsonConvert.SerializeObject(
				tile.Data.ToGeoJson((ulong)tile.Id.Z, (ulong)tile.Id.X, (ulong)tile.Id.Y),
				Formatting.Indented,
				JsonConverters.Converters
			);
			string sub = data.Length < 5000 ? data : data.Substring(0, 5000) + "\n. . . ";
			_resultsText.text = sub;
		}
	}
}