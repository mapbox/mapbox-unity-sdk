//-----------------------------------------------------------------------
// <copyright file="VectorTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using UnityEngine;

namespace Mapbox.BaseModule.Data.Tiles
{
	/// <summary>
	///    An undecoded protobuf tile: the compressed bytes as they came off the wire, in
	///    <see cref="ByteData"/>. Decoding is the consumer's job - the vector module hands these
	///    bytes to <c>Mapbox.VectorTile.VectorTile</c> in its own worker task, and the tile itself
	///    holds no decoded form.
	/// </summary>
	public abstract class ByteArrayTile : Tile
	{
		protected bool _isStyleOptimized = false;

		protected string _optimizedStyleId;

		protected string _modifiedDate;

		private byte[] byteData;
		//private TaskWrapper _task;

		public byte[] ByteData
		{
			get { return this.byteData; }
		}

		public ByteArrayTile()
		{
			_isStyleOptimized = false;
		}

		public ByteArrayTile(CanonicalTileId tileId, string tilesetId) : base(tileId, tilesetId)
		{
		}

		public ByteArrayTile(CanonicalTileId tileId, string tilesetId, string styleId, string modifiedDate) : base(tileId, tilesetId)
		{
			if (string.IsNullOrEmpty(styleId) || string.IsNullOrEmpty(modifiedDate))
			{
				UnityEngine.Debug.LogWarning("Style Id or Modified Time cannot be empty for style optimized tilesets. Switching to regular tilesets!");
				_isStyleOptimized = false;
			}
			else
			{
				_isStyleOptimized = true;
				_optimizedStyleId = styleId;
				_modifiedDate = modifiedDate;
			}
		}

		protected internal override void DoTheRequest(IFileSource fileSource)
		{
			_webRequest = fileSource.MapboxDataRequest(_generatedUrl, HandleTileResponse, ETag, 2);
		}

		public override void Cancel()
		{
			base.Cancel();
			// if (_task != null)
			// {
			// 	MapboxAccess.Instance.TaskManager.CancelTask(_task);
			// }
		}

		private void HandleTileResponse(WebRequestResponse webRequestResponse)
		{
			//unlike rastertile, we are able to null this field at the beginning of this method
			//as the data we need is stored in the response anyway and we are pretty much done with
			//request object.
			//to understand why we need to null the request object,
			//please check RasterTile.cs HandleTileResponse


			//callback has to be called here
			//otherwise requests are never complete (success or failure) and pipeline gets blocked
			
			if (webRequestResponse.Result == WebResponseResult.Success ||
			    webRequestResponse.Result == WebResponseResult.NoData)
			{
				StatusCode = webRequestResponse.StatusCode;
				byteData = webRequestResponse.Data;
				ETag = webRequestResponse.ETag;
				ExpirationDate = webRequestResponse.ExpirationDate;

				// Cancelled is not the same as loaded!
				if (TileState != TileState.Canceled)
				{
					TileState = TileState.Loaded;
				}
			}
			else if (webRequestResponse.Result == WebResponseResult.Cancelled)
			{
				TileState = TileState.Canceled;
				AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile cancelled"));
			}
			else if (webRequestResponse.Result == WebResponseResult.Retrying)
			{
				TileState = TileState.Loading;
				AddLog("Retry " + _webRequest?.TryCount);
				return;
			}
			else if (webRequestResponse.Result == WebResponseResult.Failed)
			{
				TileState = TileState.Errored;
				AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile errored"));
			}
			
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile finished"));
			_callback(new DataFetchingResult(this, webRequestResponse));
			_webRequest = null;
		}

		protected override TileResource MakeTileResource(string tilesetId)
		{

			return (_isStyleOptimized) ?
				TileResource.MakeStyleOptimizedVector(Id, tilesetId, _optimizedStyleId, _modifiedDate)
			  : TileResource.MakeVector(Id, tilesetId);
		}
	}

	public class VectorTile : ByteArrayTile
	{
		public VectorTile(CanonicalTileId canonicalTileId, string tilesetId) : base(canonicalTileId, tilesetId)
		{
			
		}
		
		protected override TileResource MakeTileResource(string tilesetId)
		{ 
			return (_isStyleOptimized) ?
				TileResource.MakeStyleOptimizedVector(Id, tilesetId, _optimizedStyleId, _modifiedDate)
				: TileResource.MakeVector(Id, tilesetId);
		}
	}
	
	public class BuildingTile : ByteArrayTile
	{
		public BuildingTile(CanonicalTileId canonicalTileId, string tilesetId) : base(canonicalTileId, tilesetId)
		{
			
		}

		protected override TileResource MakeTileResource(string tilesetId)
		{ 
			return TileResource.MakeBuilding(Id, tilesetId);
		}
	}
}
