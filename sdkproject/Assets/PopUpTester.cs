using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BundleTest
{
	public string okok;
	public BundleTest(string l)
	{
		okok = l;
	}
}

public class PopUpTester : MonoBehaviour
{
	[PopUpAttribute()]public BundleTest colors = new BundleTest("hotSauce!!!!");
	[PopUpAttribute()]public BundleTest names = new BundleTest("iceCream!!!!");//public string names;

	//public List<string> color_list = new List<string>();
	//public List<string> name_list = new List<string>();
}
