using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingRoadsScript : MonoBehaviour {

    public Material roadMat;
    
    private GameObject[] gameObject;


    public float ScrollX = 0.5f;
    public float ScrollY = 0.5f;

    public bool stopped = false;
    public float offsetSpeed = 0.5f;
    public bool reverse = false;

    private float timePassed = 0;


    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        //float OffsetX = Time.time * ScrollX;
        //float OffsetY = Time.time * ScrollY;

        float offset = Time.time * offsetSpeed % 1;
        if (reverse)
        {
            offset = -offset;
        }

        roadMat.mainTextureOffset = new Vector2(0, offset);
        






    }
}
