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
		private string _urlFormat = "https://maps.aerisapi.com/anh3TB1Xu9Wr6cPndbPwF_EuOSGuqkH433UmnajaOP0MD9rpIh5dZ38g2SUwvu/flat,ftemperatures-max-text,admin/{0}/{1}/{2}/current.png";

		public CustomImageTile(string format)
		{
			_urlFormat = format;
		}

		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			_state = State.Loading;
			_id = canonicalTileId;
			_tilesetId = tilesetId;
			_callback = p;

			fileSource.CustomImageRequest(GetURL(_id), HandleTileResponse, tileId: _id, tilesetId: tilesetId);
		}

		private string GetURL(CanonicalTileId id)
		{
			return string.Format(_urlFormat, id.Z, id.X, id.Y);
		}
	}
}
