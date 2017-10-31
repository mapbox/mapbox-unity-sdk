namespace Mapbox.Examples
{
	using System;
	using System.Linq;
	using Mapbox.Map;
	using Mapbox.Unity.Map;
	using UnityEngine;
	using UnityEngine.Events;

	/// <summary>
	/// Monobehavior Script to handle TileErrors. 
	/// There's an OnTileError event on AbstractMapVisualizer, AbstractTileFactory and UnityTile classes that one can subscribe to to listen to tile errors
	/// </summary>
	public class HandleTileErrorExample : MonoBehaviour
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

		private void _OnTileErrorHandler(TileErrorEventArgs e)
		{
			//foreach (var exception in e.Exceptions)
			//{
			//	Debug.LogError(String.Format("Exception caused on the tile. Tile ID:{0} :: {1}", e.TileId, exception));
			//}

			if (e.Exceptions.Count > 0)
			{
				Debug.LogError(String.Format(
					"{0} Exception(s) caused on the tile. Tile ID:{1}{2}{3}"
					, e.Exceptions.Count
					, e.TileId
					, Environment.NewLine
					, string.Join(Environment.NewLine, e.Exceptions.Select(ex => ex.Message).ToArray())
				));
			}


			if (OnTileError != null)
			{
				OnTileError.Invoke(e);
			}
		}

		void OnDisable()
		{
			_mapInstance.MapVisualizer.OnTileError -= _OnTileErrorHandler;
		}
	}

	[System.Serializable]
	public class TileErrorEvent : UnityEvent<TileErrorEventArgs> { }

}