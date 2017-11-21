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
			var errors = new List<Exception>();
			var warnings = new List<Exception>();

			foreach (var exception in e.Exceptions)
			{
				if (exception.Message.Contains("Request aborted"))
				{
					warnings.Add(exception);
				}
				else
				{
					errors.Add(exception);
				}
			}

			if (errors.Count > 0)
			{
				Debug.LogError(String.Format(
					"{0} Exception(s) caused on the tile. Tile ID:{1} Tile Type:{4}{2}{3}"
					, errors.Count
					, e.TileId
					, Environment.NewLine
					, string.Join(Environment.NewLine, errors.Select(ex => ex.Message).ToArray())
					, e.TileType
				));
			}

			if (warnings.Count > 0)
			{
				Debug.LogWarning(String.Format(
					"{0} Exception(s) caused on the tile. Tile ID:{1} Tile Type:{4}{2}{3}"
					, warnings.Count
					, e.TileId
					, Environment.NewLine
					, string.Join(Environment.NewLine, warnings.Select(ex => ex.Message).ToArray())
					, e.TileType
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