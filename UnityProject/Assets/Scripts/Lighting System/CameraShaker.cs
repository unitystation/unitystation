using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour {
	private Vector3 mInitialPosiiton;

	// Use this for initialization
	void Start ()
	{
		mInitialPosiiton = transform.position;
	}
	
	// Update is called once per frame
	void Update () 
	{
		transform.position = mInitialPosiiton + new Vector3(Mathf.Sin(Time.time * 0.5f) * 3f, Mathf.Sin(Time.time * 0.5f) * 3f, 0);
	}
}
