using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncPointsHolder : MonoBehaviour {

	public Vector3 [] syncPoints { get; private set; }

	public void SetSyncPoints(Vector3[] input)
	{
		syncPoints = new Vector3[input.Length];

		for(int i = 0; i < input.Length; i++)
		{
			syncPoints [i] = input [i];
		}
	}

}
