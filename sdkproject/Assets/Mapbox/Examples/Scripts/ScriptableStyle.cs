using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;

///<summary>
/// ScriptableStyle wraps a public instance of GeometryMaterialOptions into a scriptable object.
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/ScriptableStyle")]
public class ScriptableStyle : ScriptableObject
{
	public GeometryMaterialOptions geometryMaterialOptions = new GeometryMaterialOptions();
}