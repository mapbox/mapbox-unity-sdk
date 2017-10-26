using Mapbox.Unity.Map;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class MapVisualizerPerformance : MonoBehaviour
{
	private Stopwatch _sw = new Stopwatch();
	private AbstractMapVisualizer _mapVisualizer;
	private void Awake()
	{
		_mapVisualizer = FindObjectOfType<AbstractMap>().MapVisualizer;

		_mapVisualizer.OnMapVisualizerStateChanged += (s) =>
		{
			if (s == ModuleState.Working)
			{
				_sw.Reset();
				_sw.Start();
			}
			else if (s == ModuleState.Finished)
			{
				_sw.Stop();
				UnityEngine.Debug.Log(_sw.ElapsedMilliseconds);
			}
		};
	}
}
