using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
	private static SoundManager soundManager;

	private readonly Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();

	private readonly Dictionary<string, string[]> soundPatterns = new Dictionary<string, string[]>();

	private List<AudioSource> ambientTracks = new List<AudioSource>();
	public AudioSource ambientTrack => ambientTracks[0];

	// Use this for initialization
	//public AudioSource[] sounds;
	public List<AudioSource> musicTracks = new List<AudioSource>();

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
		//Ambient Volume Preference
		if (PlayerPrefs.HasKey(PlayerPrefKeys.AmbientVolumeKey))
		{
			AmbientVolume(PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey));
		}
		else
		{
			AmbientVolume(1f);
		}

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

	/// <summary>
	/// Chooses a random sound matching the given pattern if the name contains a wildcard. (#)
	/// Otherwise, it returns the same name.
	/// </summary>
	private string ResolveSoundPattern(string sndName)
	{
		if (!sounds.ContainsKey(sndName) && sndName.Contains('#'))
		{
			var soundNames = GetMatchingSounds(sndName);
			if (soundNames.Length > 0)
			{
				return soundNames[Random.Range(0, soundNames.Length)];
			}
		}
		return sndName;
	}

	/// <summary>
	/// Returns a list of known sounds that match the given pattern.
	/// </summary>
	private string[] GetMatchingSounds(string pattern)
	{
		if (soundPatterns.ContainsKey(pattern))
		{
			return soundPatterns[pattern];
		}
		var regex = new Regex(Regex.Escape(pattern).Replace(@"\#", @"\d+"));
		return soundPatterns[pattern] = sounds.Keys.Where((Func<string, bool>) regex.IsMatch).ToArray();
	}

	/// <summary>
	/// Serverside: Play sound for all clients.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayNetworked(string sndName, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30)
	{
		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.SendToAll(sndName, TransformState.HiddenPos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayNetworkedAtPos(string sndName, Vector3 pos, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30)
	{
		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.SendToAll(sndName, pos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
	}

	/// <summary>
	/// Serverside: Play sound for particular player.
	/// ("Doctor, there are voices in my head!")
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayNetworkedForPlayer(GameObject recipient, string sndName, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30)
	{
		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.Send(recipient, sndName, TransformState.HiddenPos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
	}

	/// <summary>
	/// Serverside: Play sound at given position for particular player.
	/// ("Doctor, there are voices in my head!")
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 pos, string sndName, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30)
	{
		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.Send(recipient, sndName, pos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
	}

	/// <summary>
	/// Play sound locally.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void Play(string name, float volume, float pitch = -1, float time = 0, bool oneShot = false, float pan = 0)
	{
		name = Instance.ResolveSoundPattern(name);
		if (pitch > 0)
		{
			Instance.sounds[name].pitch = pitch;
		}
		Instance.sounds[name].time = time;
		Instance.sounds[name].volume = volume;
		Instance.sounds[name].panStereo = pan;
		Play( name, oneShot );
	}

	/// <summary>
	/// Gets the sound for playing locally and allowing full control over it without
	/// having to go through sound manager. For playing local sounds only (such as in UI).
	/// </summary>
	/// <param name="name">Accepts "#" wildcards for sound variations. (Example: "Punch#")</param>
	/// <returns>audiosource of the sound</returns>
	public static AudioSource GetSound(string name)
	{
		name = Instance.ResolveSoundPattern(name);
		return Instance.sounds[name];
	}



	/// <summary>
	/// Play sound locally.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void Play(string name, bool polyphonic = false)
	{
		name = Instance.ResolveSoundPattern(name);
		var sound = Instance.sounds[name];
		if (polyphonic)
		{
			sound.PlayOneShot(sound.clip);
		}
		else
		{
			sound.Play();
		}
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayAtPosition(string name, Vector3 pos, float pitch = -1, bool polyphonic = false)
	{
		name = Instance.ResolveSoundPattern(name);
		if (Instance.sounds.ContainsKey(name))
		{
			var sound = Instance.sounds[name];
			if (pitch > 0)
			{
				sound.pitch = pitch;
			}
			sound.transform.position = pos;
			Play(name, polyphonic);
		}
	}

	/// <summary>
	/// Stops a given sound from playing locally.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void Stop(string name)
	{
		if (Instance.sounds.ContainsKey(name))
		{
			Instance.sounds[name].Stop();
		}
		else
		{
			foreach (var sound in Instance.GetMatchingSounds(name))
			{
				Instance.sounds[sound].Stop();
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
			Synth.Instance.PlayMusic(trackerMusic.Wrap(Random.Range(1, 100)), false, (byte) (int) vol);
		}
	}

	public static void PlayAmbience()
	{
		//Station hum
		Instance.ambientTrack.Play();

		//Random introduction sound
		Play( "Ambient#" );
	}

	/// <summary>
	/// Sets all ambient tracks to a certain volume
	/// </summary>
	/// <param name="volume"></param>
	public static void AmbientVolume(float volume)
	{
		foreach (AudioSource s in Instance.ambientTracks)
		{
			s.volume = volume;
		}

		PlayerPrefs.SetFloat(PlayerPrefKeys.AmbientVolumeKey, volume);
		PlayerPrefs.Save();
	}
}