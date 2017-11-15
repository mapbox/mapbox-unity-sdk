using Mapbox.Unity.MeshGeneration.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class LabelTextSetter : MonoBehaviour, IFeaturePropertySettable
{
	public void Set(Dictionary<string, object> props)
	{
		if (props.ContainsKey("name"))
			GetComponentInChildren<TextMesh>().text = props["name"].ToString();
		else if (props.ContainsKey("house_num"))
			GetComponentInChildren<TextMesh>().text = props["house_num"].ToString();
		else if (props.ContainsKey("type"))
			GetComponentInChildren<TextMesh>().text = props["type"].ToString();
	}
}
