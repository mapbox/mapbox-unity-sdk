using UnityEngine;
using System.Collections;

public class PolygonUtils
{
	/// <summary>
	/// Method to check if a point is contained inside a polygon, ignores vertical axis (y axis),
	/// </summary>
	/// <returns><c>true</c>, if point lies inside the constructed polygon, <c>false</c> otherwise.</returns>
	/// <param name="polyPoints">Polygon points.</param>
	/// <param name="p">The point that is to be tested.</param>
	static bool ContainsPoint(Vector3[] polyPoints, Vector3 p) 
	{ 
		int j = polyPoints.Length - 1;
		bool inside = false;
		for (int i = 0; i < polyPoints.Length; j = i++)
		{
			if (((polyPoints[i].y <= p.y && p.y < polyPoints[j].y) || (polyPoints[j].y <= p.y && p.y < polyPoints[i].y)) && (p.x < (polyPoints[j].x - polyPoints[i].x) * (p.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) + polyPoints[i].x))
			{
				inside = !inside;
			}
		}
		return inside; 
	}
}
