using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SoundEntry {
    public string name;
    public AudioSource source;
}

public class SoundManager : MonoBehaviour {

    public List<SoundEntry> soundsList = new List<SoundEntry>();
    private Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();

    public static SoundManager control;
	// Use this for initialization
	//public AudioSource[] sounds;
	public AudioSource[] musicTracks;
	public AudioSource[] ambientTracks;

	void Awake () {

		if (control == null) {
		
			control = this;
		
		} else {
			Destroy (this);
		}

        foreach(var s in soundsList) {
            sounds.Add(s.name, s.source);
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

    public void Play(string name, float pitch=-1, float time=0) {
        if(pitch > 0)
            sounds[name].pitch = pitch;
        sounds[name].time = time;
        sounds[name].Play();
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
