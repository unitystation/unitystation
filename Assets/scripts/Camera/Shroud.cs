﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lighting;
using System.Linq;

public class Shroud : MonoBehaviour {

	public List<LightSource> Lights;
	public float CurrentBrightness;

<<<<<<< HEAD
	// Use this for initialization
	void Start () {
		
	}
	
=======
>>>>>>> refs/remotes/unitystation/develop
	// Update is called once per frame
	void Update () {
		Lights = Lights.Where(light => light != null).ToList ();
	}

	public void AddNewLightSource(LightSource newLight) {
		if (!Lights.Contains(newLight)) {
			Lights.Add(newLight);
		}
	}
}
