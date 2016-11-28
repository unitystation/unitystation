using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour {

	public static SoundManager control;
	// Use this for initialization
	public AudioSource[] sounds;
	public AudioSource[] musicTracks;

	void Awake () {

		if (control == null) {
		
			control = this;
		
		} else {
		
		
			Destroy (control);
		
		}


	}

	public void StopMusic(){

		foreach (AudioSource track in musicTracks) {
		
			track.Stop ();
		}

	}

	public void PlayRandomTrack(){

		int randTrack = Random.Range (0, musicTracks.Length);
		musicTracks [randTrack].Play ();

	}
}
