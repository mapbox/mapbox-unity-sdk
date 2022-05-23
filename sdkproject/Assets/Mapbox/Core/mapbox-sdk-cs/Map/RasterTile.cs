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

		public bool IsTextureNonreadable;

		public RasterTile()
		{

		}

		public RasterTile(CanonicalTileId tileId, string tilesetId, bool useReadonlyTexture) : base(tileId, tilesetId)
		{
			IsTextureNonreadable = useReadonlyTexture;
		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile initialized"));
			Cancel();

			TileState = TileState.Loading;
			//Id = canonicalTileId;
			TilesetId = tilesetId;
			_callback = p;

			//we are passing etag here as well
			//if it's not null, filesource will make a `FetchTextureIfNoneMatch` request
			//else it'll be a regular request
			_unityRequest = fileSource.MapboxImageRequest(MakeTileResource(tilesetId).GetUrl(), HandleTileResponse, 10, ETag, IsTextureNonreadable);
		}

		public override void Cancel()
		{
			base.Cancel();
		}

		protected void HandleTileResponse(TextureResponse textureResponse)
		{
			//this is a callback and after this chain, unity web request will be aborted
			//and disposed as it'll hit the end of the using block in CachingWebFileSource

			if (textureResponse.HasError)
			{
				foreach (var exception in textureResponse.Exceptions)
				{
					AddException(exception);
				}

				TileState = TileState.Canceled;
			}
			else
			{
				StatusCode = textureResponse.StatusCode;
				data = textureResponse.Data;
				ETag = textureResponse.ETag;
				ExpirationDate = textureResponse.ExpirationDate;

				// Cancelled is not the same as loaded!
				if (TileState != TileState.Canceled)
				{
					TileState = TileState.Loaded;
				}
			}
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile finished"));
			_callback();
			_unityRequest = null;
			//have to null the unity request AFTER the callback as texture itself is kept
			//in the request object and request object should be kept until that's done.
			//we need to null the unity request after we are done with it though because
			//if we don't, Request.Abort line in Tile.Cancel will pop nonsense errors
			//because obviously you cannot call abort on a disposed object. It's disposed
			//as we are using `using` for webrequest objects which disposes objects in the end.
			//anyway if it's disposed but not null, `Tile.Cancel` will try to Abort() it and
			//Unity will go crazy because Unity is like that sometimes.
		}

		internal override TileResource MakeTileResource(string tilesetId)
		{
			return TileResource.MakeRaster(Id, tilesetId);
		}

		internal override bool ParseTileData(byte[] newData)
		{
			// We do not parse raster tiles as they are
			this.data = newData;
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
			TileState = TileState.Loaded;
		}

		public void ExtractTextureFromRequest()
		{
			if (_unityRequest != null)
			{
				Texture2D = DownloadHandlerTexture.GetContent(_unityRequest);
				if (Texture2D != null)
				{
					Texture2D.wrapMode = TextureWrapMode.Clamp;
				}
				else
				{
					Debug.Log("here");
				}
			}
		}
	}
}