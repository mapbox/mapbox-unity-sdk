﻿namespace Mapbox.Unity.Utilities.DebugTools
{

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	public class ExceptionLogger : MonoBehaviour
	{

		// Use this for initialization
		void Start()
		{
			Application.logMessageReceived += Application_logMessageReceived;
		}

		private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
		{
			string msg = string.Format("{0}: {1}\n{2}", type, condition, stackTrace);
			Debug.LogErrorFormat(msg);
			Console.Instance.Log(msg, "red");
			
			//if (type == LogType.Exception)
			//{
			//	Debug.LogErrorFormat("{0}: {1}\n{2}", type, condition, stackTrace);
			//}
		}

		void OnDisable()
		{
			Application.logMessageReceived -= Application_logMessageReceived;
		}

		// Update is called once per frame
		void Update()
		{

		}



	}
}
