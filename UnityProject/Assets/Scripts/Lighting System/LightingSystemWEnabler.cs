using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingSystemWEnabler : MonoBehaviour
{

	public LightingSystem mSystem;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (this.mSystem.enabled == false)
		{
			if (Time.time > 15)
				this.mSystem.enabled = true;
		}
	}
}
