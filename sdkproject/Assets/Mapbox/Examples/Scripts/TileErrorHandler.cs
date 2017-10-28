using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using UnityEngine;

/// <summary>
/// Monobehavior Script to handle TileErrors. 
/// There's an OnTileError event on AbstractMapVisualizer, AbstractTileFactory and UnityTile classes that one can subscribe to to listen to tile errors
/// </summary>
public class TileErrorHandler : MonoBehaviour {


	void OnEnable () {
		GetComponent<AbstractMap>().MapVisualizer.OnTileError += _OnTileErrorHandler;
	}

	private void _OnTileErrorHandler(Mapbox.Map.TileErrorEventArgs e)
	{
		foreach (var exception in e.Exceptions)
		{
			Debug.Log(exception);
		}
	}

	void OnDisable () {
		GetComponent<AbstractMap>().MapVisualizer.OnTileError -= _OnTileErrorHandler;
	}
}
