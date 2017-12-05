using UnityEngine;
using System.Collections;

public class DoubleClickDetectorC : MonoBehaviour {

    private int numberOfClicks = 0;
	private float timer = 0.0f;
    public float doubleClickTimeWindow = 0.3f;
	
	public bool IsDoubleClick()
    {
        var isDoubleClick = numberOfClicks == 2;
        if (isDoubleClick)
            numberOfClicks = 0;
        return isDoubleClick;
    }

    public void Update()
    {
        timer += Time.deltaTime;

        if (timer > doubleClickTimeWindow)
        {
            numberOfClicks = 0;
        }

        if (Input.GetMouseButtonDown(0))
        {
            numberOfClicks++;
            timer = 0.0f;
        }
    }
}
