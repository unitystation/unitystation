using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour {

	public static SoundManager control;
	// Use this for initialization
	public AudioSource[] sounds;
	public AudioSource[] musicTracks;
	public AudioSource[] ambientTracks;

	void Awake () {

		if (control == null) {
		
			control = this;
		
		} else {

		
			Destroy (this);
		
		}


	}

	public void StopMusic(){

		foreach (AudioSource track in musicTracks) {
		
			track.Stop ();
		}

	}

	public void StopAmbient(){
	
		foreach (AudioSource source in ambientTracks) {
		
			source.Stop ();
		
		}
	
	}

	public void PlayRandomTrack(){
		StopMusic ();
		int randTrack = Random.Range (0, musicTracks.Length);
		musicTracks [randTrack].Play ();

	}

	public void PlayVarAmbient(int variant){
	//TODO ADD MORE AMBIENT VARIANTS
		if (variant == 0) {
		
			ambientTracks [0].Play ();
			ambientTracks [1].Play ();
		
		}
	
	}
}
