using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceRotation : MonoBehaviour {

	[HideInInspector]
	public Quaternion Rotation = Quaternion.Euler(Vector3.zero);

	// Use this for initialization
	private void OnEnable()
	{
		UpdateManager.Instance.Add(CheckRotation);
	}

	private void OnDisable()
	{
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(CheckRotation);
		}
	}


	public void CheckRotation()
	{
		if (transform.rotation != Rotation)
		{
			transform.rotation = Rotation;
		}
	}
}
