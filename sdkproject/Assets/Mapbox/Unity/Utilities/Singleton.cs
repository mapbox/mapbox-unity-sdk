/**
* Copyright 2015 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/

//#define SINGLETONS_VISIBLE

namespace Mapbox.Unity.Utilities {

	using UnityEngine;
	using System;
#if NETFX_CORE
	using System.Reflection;
#endif

	/// <summary>
	/// Singleton pattern class. This class detects if T is a MonoBehavior and will 
	/// make a containing GameObject.  
	/// </summary>
	/// <typeparam name="T">The typename of the class to create as a singleton object.</typeparam>
	/// <remarks>An instance of this class needs to be of type <c>UnityEngine.Object</c> As long as this is used with UnityEngine classes, this should work fine. This is to resolve issue #116 <see href="https://github.com/mapbox/mapbox-unity-sdk/issues/116"/>	</remarks>
	public class Singleton<T> where T : UnityEngine.Object {
		#region Private Data
		static private T sm_Instance = null;
		#endregion

		#region Public Properties
		/// <summary>
		/// Returns the Singleton instance of T.
		/// </summary>
		public static T Instance {
			get {
				if (sm_Instance == null)
					CreateInstance();
				return sm_Instance;
			}
		}
		#endregion

		#region Singleton Creation
		/// <summary>
		/// Create the singleton instance.
		/// </summary>
		private static void CreateInstance() {
#if NETFX_CORE
            if (typeof(MonoBehaviour).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())) {
#else
			if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T))) {
#endif
				string singletonName = "_" + typeof(T).Name;

				GameObject singletonObject = GameObject.Find(singletonName);
				if (singletonObject == null)
					singletonObject = new GameObject(singletonName);
#if SINGLETONS_VISIBLE
                singletonObject.hideFlags = HideFlags.DontSave;
#else
				singletonObject.hideFlags = HideFlags.HideAndDontSave;
#endif
				sm_Instance = singletonObject.GetComponent<T>();
				if (sm_Instance == null)
					sm_Instance = singletonObject.AddComponent(typeof(T)) as T;
			} else {
				sm_Instance = Activator.CreateInstance(typeof(T)) as T;
			}

			if (sm_Instance == null)
				throw new Exception("Failed to create instance " + typeof(T).Name);
		}
		#endregion
	}
}