//-----------------------------------------------------------------------
// <copyright file="RasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Mapbox.Platform;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Mapbox.Map
{
	/// <summary>
	/// A raster tile from the Mapbox Style API, an encoded image representing a geographic
	/// bounding box. Usually JPEG or PNG encoded.
	/// </summary>
	/// <example>
	/// Making a RasterTile request:
	/// <code>
	/// var parameters = new Tile.Parameters();
	/// parameters.Fs = MapboxAccess.Instance;
	/// parameters.Id = new CanonicalTileId(_zoom, _tileCoorindateX, _tileCoordinateY);
	/// parameters.TilesetId = "mapbox://styles/mapbox/satellite-v9";
	/// var rasterTile = new RasterTile();
	///
	/// // Make the request.
	/// rasterTile.Initialize(parameters, (Action)(() =>
	/// {
	/// 	if (!string.IsNullOrEmpty(rasterTile.Error))
	/// 	{
	///			// Handle the error.
	///		}
	///
	/// 	// Consume the <see cref="Data"/>.
	///	}));
	/// </code>
	/// </example>
	public class RasterTile : Tile
	{
		private byte[] data;

		/// <summary> Gets the raster tile raw data. This field is only used if texture is fetched/stored as byte array. Otherwise, if it's fetched as texture, you should use Texture2D.</summary>
		/// <value> The raw data, usually an encoded JPEG or PNG. </value>
		/// <example>
		/// Consuming data in Unity to create a Texture2D:
		/// <code>
		/// var texture = new Texture2D(0, 0);
		/// texture.LoadImage(rasterTile.Data);
		/// _sampleMaterial.mainTexture = texture;
		/// </code>
		/// </example>
		public byte[] Data
		{
			get { return this.data; }
		}

		/// <summary> Gets the imagery as Texture2d object. This field is only used if texture is fetched/stored as Texture2d. Otherwise, if it's fetched as byte array, you should use Data. </summary>
		/// <value> The raw data, usually an encoded JPEG or PNG. </value>
		/// <example>
		/// <code>
		/// _sampleMaterial.mainTexture = rasterTile.Texture2D;
		/// </code>
		/// </example>
		public Texture2D Texture2D { get; private set; }

		public RasterTile()
		{

		}

		public RasterTile(CanonicalTileId tileId, string tilesetId) : base(tileId, tilesetId)
		{

		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			_state = State.Loading;
			Id = canonicalTileId;
			TilesetId = tilesetId;
			_callback = p;

			//we are passing etag here as well
			//if it's not null, filesource will make a `FetchTextureIfNoneMatch` request
			//else it'll be a regular request
			_unityRequest = fileSource.MapboxImageRequest(MakeTileResource(tilesetId).GetUrl(), HandleTileResponse, 10, Id, tilesetId, ETag);
		}

		public override void Cancel()
		{
			base.Cancel();
		}

		protected void HandleTileResponse(TextureResponse textureResponse)
		{
			//this is a callback and after this chain, unity web request will be aborted
			//and disposed as it'll hit the end of the using block in CachingWebFileSource
			_unityRequest = null;
			if (textureResponse.HasError)
			{
				foreach (var exception in textureResponse.Exceptions)
				{
					AddException(exception);
				}

				_state = State.Canceled;
			}
			else
			{
				StatusCode = textureResponse.StatusCode;
				Texture2D = textureResponse.Texture2D;
				data = textureResponse.Data;
				ETag = textureResponse.ETag;
				ExpirationDate = textureResponse.ExpirationDate;

				// Cancelled is not the same as loaded!
				if (_state != State.Canceled)
				{
					_state = State.Loaded;
				}
			}

			_callback();
		}

		internal override TileResource MakeTileResource(string tilesetId)
		{
			return TileResource.MakeRaster(Id, tilesetId);
		}

		internal override bool ParseTileData(byte[] data)
		{
			// We do not parse raster tiles as they are
			this.data = data;
			return true;
		}

		public override void Clear()
		{
			base.Clear();
			//clearing references for simplicity. It doesn't really block GC but it's clearer this way
			data = null;
			Texture2D = null;
		}

		public void SetTextureFromCache(Texture2D texture)
		{
			Texture2D = texture;
			_state = State.Loaded;
		}
	}
}