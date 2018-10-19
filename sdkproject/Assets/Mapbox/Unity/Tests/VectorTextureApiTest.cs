using System.Collections.Generic;
using Mapbox.Unity.Map;
using UnityEngine;
using UnityEngine.Assertions;
using System.Reflection;

public class VectorTextureApiTest : MonoBehaviour
{
#if !ENABLE_WINMD_SUPPORT
	private AbstractMap _abstractMap;

	private VectorSubLayerProperties _layer;

	List<System.Action> testMethods;
	List<string> testResults = new List<string>();
	private bool _testStarted;

	void Start()
	{
		_abstractMap = FindObjectOfType<AbstractMap>();
		_layer = _abstractMap.VectorData.FindFeatureSubLayerWithName("test");
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
		_layer.Texturing.RealisticStyle.SetAsStyle();
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetStyleType() == StyleTypes.Realistic);
	}

	void SetFantasyStyleType()
	{
		_layer.Texturing.FantasyStyle.SetAsStyle();
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.GetStyleType() == StyleTypes.Fantasy);
	}

	void SetSimpleStylePaletteType()
	{
		foreach (SamplePalettes palette in System.Enum.GetValues(typeof(SamplePalettes)))
		{
			_layer.Texturing.SimpleStyle.PaletteType = palette;
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.SimpleStyle.PaletteType == palette);
		}
	}

	void SetLightStyleOpacity()
	{
		float randomVal = UnityEngine.Random.value;
		_layer.Texturing.LightStyle.SetAsStyle(randomVal);
		AddResultsToList(MethodBase.GetCurrentMethod(), Mathf.Approximately(_layer.Texturing.LightStyle.Opacity, randomVal));
	}

	void SetDarkStyleOpacity()
	{
		float randomVal = UnityEngine.Random.value;
		_layer.Texturing.DarkStyle.SetAsStyle(randomVal);
		AddResultsToList(MethodBase.GetCurrentMethod(), Mathf.Approximately(_layer.Texturing.DarkStyle.Opacity, randomVal));
	}

	void SetColorStyleColor()
	{
		Color randomColor = new Color(Random.value, Random.value, Random.value, Random.value);
		_layer.Texturing.ColorStyle.SetAsStyle(randomColor);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.ColorStyle.FeatureColor == randomColor);
	}

	void SetCustomTexturingType()
	{
		_layer.Texturing.SetStyleType(StyleTypes.Custom);
		foreach (UvMapType uv in System.Enum.GetValues(typeof(UvMapType)))
		{
			_layer.Texturing.CustomStyle.TexturingType = (uv);
			AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.CustomStyle.TexturingType == uv);
		}
	}

	void SetCustomTopMaterial()
	{
		_layer.Texturing.CustomStyle.Tiled.SetAsStyle();
		Material myNewMaterial = new Material(Shader.Find("Specular"));
		_layer.Texturing.CustomStyle.Tiled.TopMaterial = (myNewMaterial);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.CustomStyle.Tiled.TopMaterial.name == myNewMaterial.name);
	}

	void SetCustomSideMaterial()
	{
		_layer.Texturing.CustomStyle.Tiled.SetAsStyle();
		Material myNewMaterial = new Material(Shader.Find("Specular"));
		_layer.Texturing.CustomStyle.Tiled.SideMaterial = (myNewMaterial);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.CustomStyle.Tiled.SideMaterial.name == myNewMaterial.name);
	}

	void SetCustomMaterials()
	{
		_layer.Texturing.CustomStyle.Tiled.SetAsStyle();
		Material myNewMaterialTop = new Material(Shader.Find("Specular"));
		Material myNewMaterialSide = new Material(Shader.Find("Specular"));
		_layer.Texturing.CustomStyle.Tiled.SetMaterials(myNewMaterialTop, myNewMaterialSide);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.CustomStyle.Tiled.TopMaterial.name == myNewMaterialTop.name);
		AddResultsToList(MethodBase.GetCurrentMethod(), _layer.Texturing.CustomStyle.Tiled.SideMaterial.name == myNewMaterialSide.name);
	}
#endif
}
