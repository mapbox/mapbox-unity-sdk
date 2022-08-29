using System;
using System.Collections.Generic;
using System.Linq;
using Collections.Hybrid.Generic;

namespace UnityEngine.XR.iOS
{
	public class UnityARAnchorManager 
	{


		private LinkedListDictionary<string, ARPlaneAnchorGameObject> planeAnchorMap;


        public UnityARAnchorManager ()
		{
			planeAnchorMap = new LinkedListDictionary<string,ARPlaneAnchorGameObject> ();
			UnityARSessionNativeInterface.ARAnchorAddedEvent += AddAnchor;
			UnityARSessionNativeInterface.ARAnchorUpdatedEvent += UpdateAnchor;
			UnityARSessionNativeInterface.ARAnchorRemovedEvent += RemoveAnchor;

		}


		public void AddAnchor(ARPlaneAnchor arPlaneAnchor)
		{
			GameObject go = UnityARUtility.CreatePlaneInScene (arPlaneAnchor);
			go.AddComponent<DontDestroyOnLoad> ();  //this is so these GOs persist across scene loads
			ARPlaneAnchorGameObject arpag = new ARPlaneAnchorGameObject ();
			arpag.planeAnchor = arPlaneAnchor;
			arpag.gameObject = go;
			planeAnchorMap.Add (arPlaneAnchor.identifier, arpag);
		}

		public void RemoveAnchor(ARPlaneAnchor arPlaneAnchor)
		{
			if (planeAnchorMap.ContainsKey (arPlaneAnchor.identifier)) {
				ARPlaneAnchorGameObject arpag = planeAnchorMap [arPlaneAnchor.identifier];
				GameObject.Destroy (arpag.gameObject);
				planeAnchorMap.Remove (arPlaneAnchor.identifier);
			}
		}

		public void UpdateAnchor(ARPlaneAnchor arPlaneAnchor)
		{
			if (planeAnchorMap.ContainsKey (arPlaneAnchor.identifier)) {
				ARPlaneAnchorGameObject arpag = planeAnchorMap [arPlaneAnchor.identifier];
				UnityARUtility.UpdatePlaneWithAnchorTransform (arpag.gameObject, arPlaneAnchor);
				arpag.planeAnchor = arPlaneAnchor;
				planeAnchorMap [arPlaneAnchor.identifier] = arpag;
			}
		}

        public void UnsubscribeEvents()
        {
            UnityARSessionNativeInterface.ARAnchorAddedEvent -= AddAnchor;
            UnityARSessionNativeInterface.ARAnchorUpdatedEvent -= UpdateAnchor;
            UnityARSessionNativeInterface.ARAnchorRemovedEvent -= RemoveAnchor;
        }

        public void Destroy()
        {
            foreach (ARPlaneAnchorGameObject arpag in GetCurrentPlaneAnchors()) {
                GameObject.Destroy (arpag.gameObject);
            }

            planeAnchorMap.Clear ();
            UnsubscribeEvents();
        }

		public LinkedList<ARPlaneAnchorGameObject> GetCurrentPlaneAnchors()
		{
			return planeAnchorMap.Values;
		}
	}
}

