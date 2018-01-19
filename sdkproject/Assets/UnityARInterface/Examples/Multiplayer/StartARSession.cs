using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartARSession : MonoBehaviour {

	public GameObject ARSetupControl;
	private bool initialized = false;

	void OnGUI()
	{
		if (!initialized) {
			//GUILayout.Box ("Put device on initializing holder and press button");
			if (GUI.Button(new Rect(Screen.width /4,  Screen.height / 4, Screen.width /2, Screen.height / 2), "Start AR Session")) {
				ARSetupControl.SetActive (true);
				initialized = true;
			}
		}
	}
	// Use this for initialization
	void Start () {
		initialized = false;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
