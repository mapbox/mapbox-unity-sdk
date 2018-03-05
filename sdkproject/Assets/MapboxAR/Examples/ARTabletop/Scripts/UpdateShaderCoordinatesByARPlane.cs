using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityARInterface;

public class UpdateShaderCoordinatesByARPlane : MonoBehaviour
{
	private Quaternion _rotation;
	private Vector3 _localScale, _position;

	void Start()
	{
		ARPlaneHandler.returnARPlane += CheckCoordinates;
		ARPlaneHandler.resetARPlane += ResetShaderValues;
	}

	void CheckCoordinates(BoundedPlane plane)
	{
		_position = plane.center;
		_rotation = Quaternion.Inverse(plane.rotation);
		_localScale = new Vector3(plane.extents.x, 10, plane.extents.y);

		UpdateShaderValues(_position, _localScale, _rotation);
	}

	void UpdateShaderValues(Vector3 position, Vector3 localScale, Quaternion rotation)
	{

		Shader.SetGlobalVector("_Origin", new Vector4(
			  position.x,
			  position.y,
			  position.z,
			  0f));
		Shader.SetGlobalVector("_BoxRotation", new Vector4(
			   rotation.eulerAngles.x,
			   rotation.eulerAngles.y,
			   rotation.eulerAngles.z,
			   0f));

		Shader.SetGlobalVector("_BoxSize", new Vector4(
			localScale.x,
			localScale.y,
			localScale.z,
			0f));
	}

	private void ResetShaderValues()
	{
		var vZero = new Vector3(0, 0, 0);
		var qZero = new Quaternion(0, 0, 0, 0);

		UpdateShaderValues(vZero, vZero, qZero);
	}

	private void OnDisable()
	{
		ARPlaneHandler.returnARPlane -= CheckCoordinates;
	}

}
