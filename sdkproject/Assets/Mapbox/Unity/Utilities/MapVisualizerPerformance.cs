namespace Mapbox.Unity.Utilities
{
	using Mapbox.Unity.Map;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using UnityEngine;

	public class MapVisualizerPerformance : MonoBehaviour
	{
		private Stopwatch _sw = new Stopwatch();
		private AbstractMap _map;
		private AbstractMapVisualizer _mapVisualizer;
		public int TestCount = 10;
		private int _currentTest = 1;
		[NonSerialized]
		public float TotalTime = 0;
		private float _firstRun;

		protected virtual void Awake()
		{
			TotalTime = 0;
			_currentTest = 1;
			_map = FindObjectOfType<AbstractMap>();
			_mapVisualizer = _map.MapVisualizer;

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
					if (_currentTest > 1)
					{
						TotalTime += _sw.ElapsedMilliseconds;
						UnityEngine.Debug.Log("Test " + _currentTest + ": " + _sw.ElapsedMilliseconds);
					}
					else
					{
						_firstRun = _sw.ElapsedMilliseconds;
					}

					if (TestCount > _currentTest)
					{
						_currentTest++;
						Invoke("Run", 1f);
					}
					else
					{
						if (_currentTest > 1)
						{
							UnityEngine.Debug.Log("First Run:        " + _firstRun + " \r\nRest Average: " + TotalTime / (_currentTest - 1));
						}
					}
				}
			};
		}

		public void Run()
		{
			//TODO : FIX THIS ERROR
			//_map.Reset();
		}
	}
}