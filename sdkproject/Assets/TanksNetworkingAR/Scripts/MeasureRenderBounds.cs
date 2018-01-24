using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeasureRenderBounds : MonoBehaviour
{

	// Update is called once per frame
	void Update ()
    {
        var totalBounds = new Bounds();
        totalBounds.SetMinMax(Vector3.one * Mathf.Infinity, -Vector3.one * Mathf.Infinity);
        foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>())
        {
            var bounds = renderer.bounds;
            var totalMin = totalBounds.min;
            totalMin.x = Mathf.Min(totalMin.x, bounds.min.x);
            totalMin.y = Mathf.Min(totalMin.y, bounds.min.y);
            totalMin.z = Mathf.Min(totalMin.z, bounds.min.z);

            var totalMax = totalBounds.max;
            totalMax.x = Mathf.Max(totalMax.x, bounds.max.x);
            totalMax.y = Mathf.Max(totalMax.y, bounds.max.y);
            totalMax.z = Mathf.Max(totalMax.z, bounds.max.z);

            totalBounds.SetMinMax(totalMin, totalMax);
        }

        Debug.LogFormat("Total size = {0}. Center = {1}", totalBounds.size, totalBounds.center);
    }
}
