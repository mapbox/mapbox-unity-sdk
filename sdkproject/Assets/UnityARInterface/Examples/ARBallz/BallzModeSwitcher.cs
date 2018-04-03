using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
	
	public class BallzModeSwitcher : MonoBehaviour {

		public GameObject ballMake;
		public GameObject ballMove;
        private GUISkin m_GuiSkin;

		private int appMode = 0;
		// Use this for initialization
		void Start () {
			m_GuiSkin = Resources.Load("GuiSkin", typeof(GUISkin)) as GUISkin;
		}
		
		// Update is called once per frame
		void Update () {
			
		}

		void EnableBallCreation(bool enable)
		{
			ballMake.SetActive (enable);
			ballMove.SetActive (!enable);
			
		}

		void OnGUI()
		{
			string modeString = appMode == 0 ? "MAKE" : "PUSH";
			if (GUI.Button(new Rect(Screen.width -150.0f, 0.0f, 150.0f, 100.0f), modeString, m_GuiSkin.button))
			{
				appMode = (appMode + 1) % 2;
				EnableBallCreation (appMode == 0);
			}

		}

	}

}