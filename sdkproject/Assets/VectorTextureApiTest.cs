//using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map; 
using Mapbox.Unity.MeshGeneration.Interfaces;
using Mapbox.Unity.MeshGeneration.Filters;
using UnityEngine;
using UnityEngine.Assertions;
using System.Reflection;

public class VectorTextureApiTest : MonoBehaviour
{
	private AbstractMap _abstractMap;

	private VectorSubLayerProperties _layer;

	List<System.Action> testMethods;
	List<string> testResults = new List<string>();
	private bool _testStarted;

	void Start()
	{
		_abstractMap = FindObjectOfType<AbstractMap>();
		_layer = _abstractMap.VectorData.LayerProperty.FindFeatureLayerWithName("test");
		Assert.IsNotNull(_layer, "No layer named test found");

		testMethods = new List<System.Action>
		{
			SetStyle, 
			SetRealisticStyleType, 
			SetFantasyStyleType,
			SetSimpleStylePaletteType,
			SetLightStyleOpacity,
			SetDarkStyleOpacity,
			SetColorStyleColor,
			SetCustomTexturingType,
			SetCustomTopMaterial,
			SetCustomSideMaterial,
			SetCustomMaterials
		};
	}

	private void ConductTests()
	{
		for (int i = 0; i < testMethods.Count; i++)
		{
			testMethods[i]();
		}
		PrintResults();
	}

	private void Update()
	{
		if(_testStarted)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			ConductTests();
			_testStarted = true;
		}
	}

	private void AddResultsToList(MethodBase methodBase, bool result)
	{
		string color = (result) ? "cyan" : "red";
		string printStatement = string.Format("<color={0}>{1} -> {2}</color>", color, result, methodBase.Name);
		testResults.Add(printStatement);
	}

	private void PrintResults()
	{
		Debug.Log("<color=yellow>Vector Texture API Test ///////////////////////////////////////////////////</color>");
		for (int i = 0; i < testResults.Count; i++)
		{
			Debug.Log(testResults[i]);
		}
	}

	void SetStyle()
	{
		foreach (StyleTypes style in System.Enum.GetValues(typeof(StyleTypes)))
		{
			_layer.SetStyleType(style);
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetStyleType() == style);
		}
	}

	void SetRealisticStyleType()
	{
		_layer.SetRealisticStyle();
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetStyleType() == StyleTypes.Realistic);
	}

	void SetFantasyStyleType()
	{
		_layer.SetFantasyStyle();
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetStyleType() == StyleTypes.Fantasy);
	}

	void SetSimpleStylePaletteType()
	{
		foreach (SamplePalettes palette in System.Enum.GetValues(typeof(SamplePalettes)))
		{
			_layer.SetSimpleStylePaletteType(palette);
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetSimpleStylePaletteType() == palette);
		}
	}

	void SetLightStyleOpacity()
	{
		float randomVal = UnityEngine.Random.value;
		_layer.SetLightStyleOpacity(randomVal);
		AddResultsToList(MethodBase.GetCurrentMethod(), Mathf.Approximately(_layer.GetLightStyleOpacity(), randomVal));
	}

	void SetDarkStyleOpacity()
	{
		float randomVal = UnityEngine.Random.value;
		_layer.SetDarkStyleOpacity(randomVal);
		AddResultsToList(MethodBase.GetCurrentMethod(), Mathf.Approximately(_layer.GetDarkStyleOpacity(), randomVal));
	}

	void SetColorStyleColor()
	{
		Color randomColor = new Color(Random.value, Random.value, Random.value, Random.value);
		_layer.SetColorStyleColor(randomColor);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetColorStyleColor() == randomColor);
	}

	void SetCustomTexturingType()
	{
		foreach (UvMapType uv in System.Enum.GetValues(typeof(UvMapType)))
		{
			_layer.SetCustomTexturingType(uv);
			Debug.Log("<color=yellow>" + uv.ToString() + " - " + _layer.GetCustomTexturingType().ToString() + "</color>");
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetCustomTexturingType() == uv);
		}
	}

	void SetCustomTopMaterial()
	{
		Material myNewMaterial = new Material(Shader.Find("Specular"));
		if(myNewMaterial == null)
		{
			Debug.Log("efmo");
		}
		_layer.SetCustomTopMaterial(myNewMaterial);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetCustomTopMaterial() == myNewMaterial);
	}

	void SetCustomSideMaterial()
	{
		Material myNewMaterial = new Material(Shader.Find("Specular"));
		if (myNewMaterial == null)
		{
			Debug.Log("efmo");
		}
		_layer.SetCustomSideMaterial(myNewMaterial);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetCustomSideMaterial() == myNewMaterial);
	}

	void SetCustomMaterials()
	{
		Material myNewMaterialTop = new Material(Shader.Find("Specular"));
		Material myNewMaterialSide = new Material(Shader.Find("Specular"));
		_layer.SetCustomMaterials(myNewMaterialTop, myNewMaterialSide);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetCustomTopMaterial() == myNewMaterialTop);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.GetCustomSideMaterial() == myNewMaterialSide);
	}
}