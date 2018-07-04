namespace Mapbox.Experimental.Platform.Http
{

	using System;
	using System.Net;

	public class MapboxHttpResponseReceivedEventArgs : EventArgs
	{
		public MapboxHttpResponseReceivedEventArgs
		(
			object id
			, MapboxHttpResponse response
		)
		{
			Id = id;
			Response = response;
			Completed = null != response.Data;
			Succeeded = Completed && response.StatusCode.HasValue && response.StatusCode.Value == (int)HttpStatusCode.OK;
		}


		public object Id { get; private set; }


		public MapboxHttpResponse Response { get; private set; }

		public bool Completed { get; private set; }

		public bool Succeeded { get; private set; }
	}



	public class MapboxWebDataFetcherResponseReceivedEventArgs : EventArgs
	{

		public MapboxWebDataFetcherResponseReceivedEventArgs(
			MapboxHttpResponseReceivedEventArgs responseReceivedEventArgs
			, int requestsInQueue
			, int requestsExecuting
		)
		{
			ResponseEventArgs = responseReceivedEventArgs;
			RequestsInQueue = requestsInQueue;
		}


		public MapboxHttpResponseReceivedEventArgs ResponseEventArgs { get; private set; }
		public int RequestsInQueue { get; private set; }
		public int RequestsExecuting { get; private set; }
	}

}
