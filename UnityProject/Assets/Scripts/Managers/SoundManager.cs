﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class SoundEntry
{
	public string name;
	public AudioSource source;
}

public class SoundManager : MonoBehaviour
{
	private static SoundManager soundManager;

	private readonly Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource> ();

	public AudioSource[] ambientTracks;

	// Use this for initialization
	//public AudioSource[] sounds;
	public AudioSource[] musicTracks;

	public List<SoundEntry> soundsList = new List<SoundEntry> ();
	public int ambientPlaying { get; private set; }

	public static SoundManager Instance
	{
		get
		{
			if (!soundManager)
			{
				soundManager = FindObjectOfType<SoundManager> ();
				soundManager.Init ();
			}

			return soundManager;
		}
	}

	[Range(0f, 1f)]
	public float MusicVolume = 1;

	public AudioSource this [string key]
	{
		get
		{
			AudioSource source;
			return sounds.TryGetValue (key, out source) ? source : null;
		}
	}

	private void Init ()
	{
		// add sounds to sounds dictionary
		foreach (SoundEntry s in soundsList)
		{
			sounds.Add (s.name, s.source);
		}
	}

	public static void StopMusic ()
	{
		foreach (AudioSource track in Instance.musicTracks)
		{
			track.Stop ();
		}
		Synth.Instance.StopMusic ();
	}

	public static void StopAmbient ()
	{
		foreach (AudioSource source in Instance.ambientTracks)
		{
			source.Stop ();
		}
	}

	public static void Play (string name, float volume, float pitch = -1, float time = 0)
	{
		if (pitch > 0)
		{
			Instance.sounds[name].pitch = pitch;
		}
		Instance.sounds[name].time = time;
		Instance.sounds[name].volume = volume;
		Instance.sounds[name].Play ();
		//Set to cache incase it was changed
	}

	public static void Play (string name)
	{
		Instance.sounds[name].Play ();
	}

	public static void Stop (string name)
	{
		if (Instance.sounds.ContainsKey (name))
		{
			Instance.sounds[name].Stop ();
		}
	}

	public static void PlayAtPosition (string name, Vector3 pos, float pitch = -1)
	{
		if (Instance.sounds.ContainsKey (name))
		{
			if (pitch > 0)
			{
				Instance.sounds[name].pitch = pitch;
			}
			Instance.sounds[name].transform.position = pos;
			Instance.sounds[name].Play ();
			//Set to cache incase it was changed
		}
	}

	[ContextMenu("PlayRandomMusicTrack")]
	public void PlayRndTrackEditor(){
		PlayRandomTrack();
	}

	public static void PlayRandomTrack ()
	{
		StopMusic ();
		if (Random.Range (0, 2).Equals (0))
		{
			//Traditional music
			int randTrack = Random.Range (0, Instance.musicTracks.Length);
			Instance.musicTracks[randTrack].volume = Instance.MusicVolume;
			Instance.musicTracks[randTrack].Play ();
		}
		else
		{
			//Tracker music
			var trackerMusic = new []
			{
				"spaceman.xm",
				"echo_sound.xm",
				"tintin.xm"
			};
			var vol = 255 * Instance.MusicVolume;
			Synth.Instance.PlayMusic (trackerMusic.Wrap (Random.Range (1, 100)),false, (byte)(int)vol);
		}
	}

	public static void PlayVarAmbient (int variant)
	{
		//TODO ADD MORE AMBIENT VARIANTS
		if (variant == 0)
		{
			//Station ambience with announcement at start
			Instance.ambientTracks[2].Stop ();
			Instance.ambientTracks[0].Play ();
			Instance.ambientTracks[1].Play ();
			Instance.ambientPlaying = 1;
		}
		if (variant == 1)
		{
			Instance.ambientTracks[0].Stop ();
			Instance.ambientTracks[1].Play ();
			Instance.ambientTracks[2].Play ();
			Instance.ambientPlaying = 1;
		}

		if (variant == 2)
		{
			Instance.ambientTracks[2].Stop ();
			Instance.ambientTracks[3].Play ();
			Instance.ambientTracks[1].Play ();
			Instance.ambientPlaying = 1;
		}
	}

	public static void AmbientVolume (float volume)
	{
		Instance.ambientTracks[Instance.ambientPlaying].volume = volume;
	}
}