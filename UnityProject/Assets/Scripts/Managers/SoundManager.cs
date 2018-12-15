using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
	private static SoundManager soundManager;

	private readonly Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();

	public List<AudioSource> ambientTracks = new List<AudioSource>();

	// Use this for initialization
	//public AudioSource[] sounds;
	public List<AudioSource> musicTracks = new List<AudioSource>();

	public int ambientPlaying { get; private set; }

	public static SoundManager Instance
	{
		get
		{
			if (!soundManager)
			{
				soundManager = FindObjectOfType<SoundManager>();
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
			return sounds.TryGetValue(key, out source) ? source : null;
		}
	}

	void Awake()
	{
		Init();
	}

	private void Init()
	{
		// Cache all sounds in the tree
		var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
		for (int i = 0; i < audioSources.Length; i++)
		{
			if (audioSources[i].gameObject.tag == "AmbientSound")
			{
				ambientTracks.Add(audioSources[i]);
				continue;

			}
			if (audioSources[i].gameObject.tag == "Music")
			{
				musicTracks.Add(audioSources[i]);
				continue;
			}
			if (audioSources[i].gameObject.tag == "SoundFX")
			{
				sounds.Add(audioSources[i].name, audioSources[i]);
			}
		}
	}

	public static void StopMusic()
	{
		foreach (AudioSource track in Instance.musicTracks)
		{
			track.Stop();
		}
		Synth.Instance.StopMusic();
	}

	public static void StopAmbient()
	{
		foreach (AudioSource source in Instance.ambientTracks)
		{
			source.Stop();
		}
	}

	public static void Play(string name, float volume, float pitch = -1, float time = 0)
	{
		if (pitch > 0)
		{
			Instance.sounds[name].pitch = pitch;
		}
		Instance.sounds[name].time = time;
		Instance.sounds[name].volume = volume;
		Instance.sounds[name].Play();
		//Set to cache incase it was changed
	}

	public static void Play(string name)
	{
		Instance.sounds[name].Play();
	}

	public static void Stop(string name)
	{
		if (Instance.sounds.ContainsKey(name))
		{
			Instance.sounds[name].Stop();
		}
	}

	public static void PlayAtPosition(string name, Vector3 pos, float pitch = -1)
	{
		if (Instance.sounds.ContainsKey(name))
		{
			if (pitch > 0)
			{
				Instance.sounds[name].pitch = pitch;
			}
			Instance.sounds[name].transform.position = pos;
			Instance.sounds[name].Play();
			//Set to cache incase it was changed
		}
	}

	[ContextMenu("PlayRandomMusicTrack")]
	public void PlayRndTrackEditor()
	{
		PlayRandomTrack();
	}

	public static void PlayRandomTrack()
	{
		StopMusic();
		if (Random.Range(0, 2).Equals(0))
		{
			//Traditional music
			int randTrack = Random.Range(0, Instance.musicTracks.Count);
			Instance.musicTracks[randTrack].volume = Instance.MusicVolume;
			Instance.musicTracks[randTrack].Play();
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
			Synth.Instance.PlayMusic(trackerMusic.Wrap(Random.Range(1, 100)), false, (byte)(int)vol);
		}
	}

	public static void PlayVarAmbient(int variant)
	{
		//TODO ADD MORE AMBIENT VARIANTS
		if (variant == 0)
		{
			//Station ambience with announcement at start
			Instance.ambientTracks[2].Stop();
			Instance.ambientTracks[0].Play();
			Instance.ambientTracks[1].Play();
			Instance.ambientPlaying = 1;
		}
		if (variant == 1)
		{
			Instance.ambientTracks[0].Stop();
			Instance.ambientTracks[1].Play();
			Instance.ambientTracks[2].Play();
			Instance.ambientPlaying = 1;
		}

		if (variant == 2)
		{
			Instance.ambientTracks[2].Stop();
			Instance.ambientTracks[3].Play();
			Instance.ambientTracks[1].Play();
			Instance.ambientPlaying = 1;
		}
	}

	public static void AmbientVolume(float volume)
	{
		Instance.ambientTracks[Instance.ambientPlaying].volume = volume;
	}
}