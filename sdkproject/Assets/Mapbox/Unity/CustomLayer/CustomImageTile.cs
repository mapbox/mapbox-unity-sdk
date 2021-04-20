using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Platform;
using UnityEngine;

namespace CustomImageLayerSample
{
	public class CustomImageTile : RasterTile
	{
		private string _urlFormat = "https://maps.aerisapi.com/(API_KEY_HERE)/flat,ftemperatures-max-text,admin/{0}/{1}/{2}/current.png";

		public CustomImageTile(CanonicalTileId tileId, string tilesetId, string format) : base(tileId, tilesetId)
		{
			_urlFormat = format;
		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			_state = State.Loading;
			Id = canonicalTileId;
			TilesetId = tilesetId;
			_callback = p;

			_unityRequest = fileSource.CustomImageRequest(GetURL(Id), HandleTileResponse, tileId: Id, tilesetId: tilesetId);
		}

		private string GetURL(CanonicalTileId id)
		{
			return string.Format(_urlFormat, id.Z, id.X, id.Y);
		}


	}

	public class FileImageTile : RasterTile
	{
		public string FilePath;

		public FileImageTile(CanonicalTileId tileId, string tilesetId, string filePath) : base(tileId, tilesetId)
		{
			FilePath = filePath;
		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			_state = State.Loading;
			Id = canonicalTileId;
			TilesetId = tilesetId;
			_callback = p;

			_unityRequest = fileSource.CustomImageRequest(FilePath, HandleTileResponse, tileId: Id, tilesetId: tilesetId);
		}
	}
}
