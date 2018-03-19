namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Mapbox.Map;
	using Mapbox.Unity.Map;
	using UnityEngine;
	using UnityEngine.Events;

	/// <summary>
	/// Monobehavior Script to handle TileErrors. 
	/// There's an OnTileError event on AbstractMapVisualizer, AbstractTileFactory and UnityTile classes that one can subscribe to to listen to tile errors
	/// </summary>
	public class TileErrorHandler : MonoBehaviour
	{

		[SerializeField]
		private AbstractMap _mapInstance;
		public TileErrorEvent OnTileError;

		void OnEnable()
		{
			if (_mapInstance == null)
			{
				_mapInstance = GetComponent<AbstractMap>();
			}

			_mapInstance.MapVisualizer.OnTileError += _OnTileErrorHandler;
		}

		private void _OnTileErrorHandler(object sender, TileErrorEventArgs e)
		{
			// check if request has been aborted: show warning not error
			if (e.Exceptions.Count > 0)
			{
				// 1. aborted is always the first exception
				//    additional exceptions are always caused by the request being aborted
				//    show all of them as warnings
				// 2. 'Unable to write data' is another exception associated 
				//    with aborted requests: request finshed successfully but
				//    was aborted during filling of local buffer, also show as warning
				if (
					e.Exceptions[0].Message.Contains("Request aborted")
					|| e.Exceptions[0].Message.Equals("Unable to write data")
				)
				{
					Debug.LogWarning(printMessage(e.Exceptions, e));
				}
				else
				{
					Debug.LogError(printMessage(e.Exceptions, e));
				}
			}

			if (OnTileError != null)
			{
				OnTileError.Invoke(e);
			}
		}

		private string printMessage(List<Exception> exceptions, TileErrorEventArgs e)
		{
			return string.Format(
				"{0} Exception(s) caused on the tile. Tile ID:{1} Tile Type:{4}{2}{3}"
				, exceptions.Count
				, e.TileId
				, Environment.NewLine
				, string.Join(Environment.NewLine, exceptions.Select(ex => ex.Message).ToArray())
				, e.TileType
			);
		}

		void OnDisable()
		{
			_mapInstance.MapVisualizer.OnTileError -= _OnTileErrorHandler;
		}
	}

	[System.Serializable]
	public class TileErrorEvent : UnityEvent<TileErrorEventArgs> { }

}