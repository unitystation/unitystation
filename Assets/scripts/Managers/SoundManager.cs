﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class SoundEntry {
    public string name;
    public AudioSource source;
}

public class SoundManager: MonoBehaviour {

    public List<SoundEntry> soundsList = new List<SoundEntry>();
    private Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();
    // Use this for initialization
    //public AudioSource[] sounds;
    public AudioSource[] musicTracks;
    public AudioSource[] ambientTracks;
	public int ambientPlaying { get; private set; }

    private static SoundManager soundManager;
   
    public static SoundManager Instance {
        get {
            if(!soundManager) {
                soundManager = FindObjectOfType<SoundManager>();
                soundManager.Init();
            }

            return soundManager;
        }
    }

	public AudioSource this [string key] {
		get {
			AudioSource source;
			return sounds.TryGetValue(key, out source) ? source : null;
		}
	}

    private void Init() {
        // add sounds to sounds dictionary
        foreach(var s in soundsList) {
            sounds.Add(s.name, s.source);
        }
    }

    public static void StopMusic() {
        foreach(AudioSource track in Instance.musicTracks) {
            track.Stop();
        }
    }

    public static void StopAmbient() {
        foreach(AudioSource source in Instance.ambientTracks) {
            source.Stop();
        }
    }

    public static void Play(string name, float volume = 1, float pitch = -1, float time = 0) {
        if(pitch > 0)
            Instance.sounds[name].pitch = pitch;
        Instance.sounds[name].time = time;
        Instance.sounds[name].volume = volume;
        Instance.sounds[name].Play();
    }

	public static void Stop(string name){
		if(Instance.sounds.ContainsKey(name)){
		Instance.sounds[name].Stop();
		}
	}

    public static void PlayAtPosition(string name, Vector3 pos){
        if(Instance.sounds.ContainsKey(name)){
            Instance.sounds[name].transform.position = pos;
            Instance.sounds[name].Play();
        }
    }

    public static void PlayRandomTrack() {
        StopMusic();
        int randTrack = Random.Range(0, Instance.musicTracks.Length);
        Instance.musicTracks[randTrack].Play();
    }

    public static void PlayVarAmbient(int variant) {
        //TODO ADD MORE AMBIENT VARIANTS
        if(variant == 0) {
			//Station ambience with announcement at start
            Instance.ambientTracks[0].Play();
            Instance.ambientTracks[1].Play();
			Instance.ambientPlaying = 1;
        }
    }

	public static void AmbientVolume(float volume){
		Instance.ambientTracks[Instance.ambientPlaying].volume = volume;
	}
}
