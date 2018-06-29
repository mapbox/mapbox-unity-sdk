namespace Mapbox.Experimental.Platform.Http
{

	using System;
	using System.Net;

	public class MapboxHttpResponseReceivedEventArgs : EventArgs
	{


		public MapboxHttpResponseReceivedEventArgs(
			object id
			, MapboxHttpResponse response
			)
		{
			Completed = null != response.Data;
			Succeeded = Completed && response.StatusCode.HasValue && response.StatusCode.Value == (int)HttpStatusCode.OK;
		}


		public object Id { get; set; }

		public bool Completed { get; set; }

		public bool Succeeded { get; set; }
	}
}
