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
		_layer = _abstractMap.VectorData.FindFeatureLayerWithName("test");
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
		if (_testStarted)
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
			_layer.Texturing.SetStyleType(style);
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetStyleType() == style);
		}
	}

	void SetRealisticStyleType()
	{
		_layer.Texturing.SetRealisticStyle();
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetStyleType() == StyleTypes.Realistic);
	}

	void SetFantasyStyleType()
	{
		_layer.Texturing.SetFantasyStyle();
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetStyleType() == StyleTypes.Fantasy);
	}

	void SetSimpleStylePaletteType()
	{
		foreach (SamplePalettes palette in System.Enum.GetValues(typeof(SamplePalettes)))
		{
			_layer.Texturing.SetSimpleStylePaletteType(palette);
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetSimpleStylePaletteType() == palette);
		}
	}

	void SetLightStyleOpacity()
	{
		float randomVal = UnityEngine.Random.value;
		_layer.Texturing.SetLightStyleOpacity(randomVal);
		AddResultsToList(MethodBase.GetCurrentMethod(), Mathf.Approximately(_layer.Texturing.GetLightStyleOpacity(), randomVal));
	}

	void SetDarkStyleOpacity()
	{
		float randomVal = UnityEngine.Random.value;
		_layer.Texturing.SetDarkStyleOpacity(randomVal);
		AddResultsToList(MethodBase.GetCurrentMethod(), Mathf.Approximately(_layer.Texturing.GetDarkStyleOpacity(), randomVal));
	}

	void SetColorStyleColor()
	{
		Color randomColor = new Color(Random.value, Random.value, Random.value, Random.value);
		_layer.Texturing.SetColorStyleColor(randomColor);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetColorStyleColor() == randomColor);
	}

	void SetCustomTexturingType()
	{
		_layer.Texturing.SetStyleType(StyleTypes.Custom);
		foreach (UvMapType uv in System.Enum.GetValues(typeof(UvMapType)))
		{
			_layer.Texturing.SetTexturingType(uv);
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetTexturingType() == uv);
		}
	}

	void SetCustomTopMaterial()
	{
		_layer.Texturing.SetStyleType(StyleTypes.Custom);
		Material myNewMaterial = new Material(Shader.Find("Specular"));
		_layer.Texturing.SetTopMaterial(myNewMaterial);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetTopMaterial().name == myNewMaterial.name);
	}

	void SetCustomSideMaterial()
	{
		_layer.Texturing.SetStyleType(StyleTypes.Custom);
		Material myNewMaterial = new Material(Shader.Find("Specular"));
		_layer.Texturing.SetSideMaterial(myNewMaterial);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetSideMaterial().name == myNewMaterial.name);
	}

	void SetCustomMaterials()
	{
		_layer.Texturing.SetStyleType(StyleTypes.Custom);
		Material myNewMaterialTop = new Material(Shader.Find("Specular"));
		Material myNewMaterialSide = new Material(Shader.Find("Specular"));
		_layer.Texturing.SetMaterials(myNewMaterialTop, myNewMaterialSide);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetTopMaterial().name == myNewMaterialTop.name);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetSideMaterial().name == myNewMaterialSide.name);
	}
}