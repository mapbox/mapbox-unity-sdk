using System;
using Mapbox.Map;
using Mapbox.Platform;

namespace Mapbox.Unity.CustomLayer
{
	public class CustomImageTile : RasterTile
	{
		private string _urlFormat = "https://maps.aerisapi.com/(API_KEY_HERE)/flat,ftemperatures-max-text,admin/{0}/{1}/{2}/current.png";

		public CustomImageTile(CanonicalTileId tileId, string tilesetId, string format) : base(tileId, tilesetId, true)
		{
			_urlFormat = format;
		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			TileState = TileState.Loading;
			Id = canonicalTileId;
			TilesetId = tilesetId;
			_callback = p;

			_unityRequest = fileSource.CustomImageRequest(GetURL(Id), HandleTileResponse);
		}

		private string GetURL(CanonicalTileId id)
		{
			return string.Format(_urlFormat, id.Z, id.X, id.Y);
		}


	}

	public class FileImageTile : RasterTile
	{
		public string FilePath;

		public FileImageTile(CanonicalTileId tileId, string tilesetId, string filePath, bool isTextureNonreadable) : base(tileId, tilesetId, isTextureNonreadable)
		{
			FilePath = filePath;
		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			TileState = TileState.Loading;
			Id = canonicalTileId;
			TilesetId = tilesetId;
			_callback = p;

			_unityRequest = fileSource.CustomImageRequest(FilePath, HandleTileResponse, 10, Id, tilesetId, ETag, IsTextureNonreadable);
		}
	}
}
