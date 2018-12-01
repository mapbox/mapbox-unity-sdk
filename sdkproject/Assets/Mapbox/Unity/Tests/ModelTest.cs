using Mapbox.Unity.Map;
using UnityEngine;

public class ModelTest : MonoBehaviour
{
	private AbstractMap _abstractMap;

	void Start()
	{
		_abstractMap = FindObjectOfType<AbstractMap>();
	}

	[ContextMenu("Disable Extrusion")]
	public void DisableExtrusion()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.DisableExtrusion();
		}
	}

	[ContextMenu("Set Absolute Extrusion")]
	public void SetAbsoluteExtrusion()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.EnableAbsoluteExtrusion(ExtrusionGeometryType.RoofAndSide, 10, 1);
		}
	}

	[ContextMenu("Set Property Extrusion")]
	public void SetPropertyExtrusion()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.EnablePropertyExtrusion(ExtrusionGeometryType.RoofAndSide);
		}
	}

	[ContextMenu("Set Minimum Height Extrusion")]
	public void SetMinimumExtrusion()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.EnableMinExtrusion(ExtrusionGeometryType.RoofAndSide);
		}
	}

	[ContextMenu("Set Maximum Height Extrusion")]
	public void SetMaximumExtrusion()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.EnableMaxExtrusion(ExtrusionGeometryType.RoofAndSide);
		}
	}

	[ContextMenu("Set Range Extrusion")]
	public void SetRangeExtrusion()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.EnableRangeExtrusion(ExtrusionGeometryType.RoofAndSide, 10, 20);
		}
	}

	[ContextMenu("Set Absolute Height to 35")]
	public void SetAbsoluteHeight()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.SetAbsoluteHeight(35);
		}
	}

	[ContextMenu("Set Height Range to 35-70")]
	public void SetHeightRange()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.SetHeightRange(35, 70);
		}
	}

	[ContextMenu("Set Extrusion Multiplier to x2")]
	public void SetExtrusionMultiplier()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ExtrusionOptions.SetExtrusionMultiplier(2);
		}
	}

	[ContextMenu("Enable terrain snapping")]
	public void EnableTerrainSnapping()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.EnableSnapingTerrain(!layer.coreOptions.snapToTerrain);
		}
	}

	[ContextMenu("Enable mesh combining")]
	public void EnableMeshCombining()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.EnableCombiningMeshes(true);
		}
	}

	[ContextMenu("Set Feature Collider")]
	public void SetFeatureCollider()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.ColliderOptions.SetFeatureCollider((ColliderType)UnityEngine.Random.Range(0, 4));
		}
	}

	[ContextMenu("Set Primitive Type")]
	public void SetPrimitiveType()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllFeatureSubLayers())
		{
			layer.Modeling.CoreOptions.SetPrimitiveType((VectorPrimitiveType)UnityEngine.Random.Range(0, 2));
		}
	}

	[ContextMenu("Set Line Width to 10")]
	public void SetLineWidth()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllLineFeatureSubLayers())
		{
			layer.Modeling.LineOptions.SetLineWidth(10);
		}
	}

	[ContextMenu("Toggle Line Join Type")]
	public void SetLineJoinType()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllLineFeatureSubLayers())
		{

			layer.Modeling.LineOptions.SetJoinType((LineJoinType) ((((int)(LineJoinType)layer.lineGeometryOptions.JoinType) + 1) % 3));
		}
	}

	[ContextMenu("Toggle Line Cap Type")]
	public void SetLineCapType()
	{
		foreach (var layer in _abstractMap.VectorData.GetAllLineFeatureSubLayers())
		{
			layer.Modeling.LineOptions.SetCapType((LineCapType) ((((int)(LineCapType)layer.lineGeometryOptions.CapType) + 1) % 3));
		}
	}
}