using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lighting;
using System.Linq;
using Events;

public class Shroud : MonoBehaviour {

	public List<LightSource> Lights;
	public float CurrentBrightness;

	void OnEnable(){
		EventManager.LightUpdate += UpdateLightSources;	
	}

	void OnDisable(){
		EventManager.LightUpdate -= UpdateLightSources;	
	}

	public void UpdateLightSources() {
		Lights = Lights.Where(light => (light != null)).ToList ();
		Lights = Lights.Where(light => light.gameObject.activeSelf == true).ToList ();
		Lights = Lights.Where(light => light.LightOn == true).ToList ();
	}

	public void AddNewLightSource(LightSource newLight) {
		if (!Lights.Contains(newLight)) {
			Lights.Add(newLight);
		}
	}
}
