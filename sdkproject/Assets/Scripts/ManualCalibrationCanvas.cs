using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualCalibrationCanvas : MonoBehaviour {


    public static ManualCalibrationCanvas Instance = null;
	// Use this for initialization
	void Start () {
        Instance = this;
        Instance.gameObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {


		
	}
}
