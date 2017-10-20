using UnityEngine;
using System.Collections;

public class Scannable : MonoBehaviour
{
	public Animator UIAnim;

	private void Start()
	{
		UIAnim = GetComponent<Animator>();
	}

	public void Ping()
	{
		UIAnim.SetTrigger("Ping");
    }
}
