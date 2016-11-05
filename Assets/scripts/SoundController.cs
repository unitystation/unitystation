using UnityEngine;
using System.Collections;

public class SoundController : MonoBehaviour {

	public static SoundController control;
	// Use this for initialization
	public AudioSource[] sounds;

	void Awake () {

		if (control == null) {
		
			control = this;
		
		} else {
		
		
			Destroy (control);
		
		}


	}
}
