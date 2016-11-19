using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour {

	public static SoundManager control;
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
