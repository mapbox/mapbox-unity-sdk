using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectRadius : MonoBehaviour
{
    public float areaOfEffect = 1;

    [SerializeField]
    public GameObject map;

    void Start() {
        map = GameObject.Find("Map");
    }
    void OnDrawGizmos() {
		// Gizmos.color = gizmoColor;
        // Gizmos.DrawWireSphere(transform.position, gizmoRadius);
		Gizmos.DrawIcon(transform.position + Vector3.up * 3f, "informationIcon.png", true);
        map = GameObject.Find("Map");
    }
}
