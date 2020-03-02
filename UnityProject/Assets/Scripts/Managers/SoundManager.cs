using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
	public AudioMixerGroup DefaultMixer;

	public AudioMixerGroup MuffledMixer;

	private static LayerMask layerMask;

	private static SoundManager soundManager;

	private readonly Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();

	private readonly Dictionary<string, string[]> soundPatterns = new Dictionary<string, string[]>();

	private static readonly System.Random RANDOM = new System.Random();

	private static AudioSource currentLobbyAudioSource;

	private readonly Dictionary<Footstep, List<string>> FootSteps = new Dictionary<Footstep, List<string>>(){
		{ Footstep.floor,
			 new List<string> {"floor1","floor2","floor3","floor4","floor5"}},
		{Footstep.asteroid,
			 new List<string> {"asteroid1","asteroid2","asteroid3","asteroid4","asteroid5"}},
		{Footstep.carpet,
			 new List<string> {"carpet1","carpet2","carpet3","carpet4","carpet5"}},
		{Footstep.catwalk,
			 new List<string> {"catwalk1","catwalk2","catwalk3","catwalk4","catwalk5"}},
		{Footstep.grass,
			 new List<string> {"grass1","grass2","grass3","grass4"}},
		{Footstep.lava, //not literally
			 new List<string> {"lava1","lava2","lava3"}},
		{Footstep.plating,
			 new List<string> {"plating1","plating2","plating3","plating4", "plating5" }},
		{Footstep.wood,
			 new List<string> {"wood1","wood2","wood3","wood4", "wood5" }},
		{Footstep.sand,
			 new List<string> {"asteroid1","asteroid2","asteroid3","asteroid4","asteroid5"}},
		{Footstep.water,
			 new List<string> {"water1", "water2", "water3", "water4"}},
		{Footstep.clownstep,
			 new List<string> {"clownstep1","clownstep2" }},
	};

	private readonly Dictionary<BareFootstep, List<string>> BareFootsteps = new Dictionary<BareFootstep, List<string>>(){
		{BareFootstep.floor,
			 new List<string> {"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}},
		{BareFootstep.asteroid,
			 new List<string> {"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}},
		{BareFootstep.carpet,
			 new List<string> {"carpetbarefoot1", "carpetbarefoot2", "carpetbarefoot3", "carpetbarefoot4", "carpetbarefoot5"}},
		{BareFootstep.catwalk,
			 new List<string> {"catwalk1","catwalk2","catwalk3","catwalk4","catwalk5"}},
		{BareFootstep.grass,
			 new List<string> {"grass1","grass2","grass3","grass4"}},
		{BareFootstep.lava, //not literally
			 new List<string> {"lava1","lava2","lava3"}},
		{BareFootstep.plating,
			new List<string> {"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}},
		{BareFootstep.wood,
			 new List<string> {"woodbarefoot1", "woodbarefoot2", "woodbarefoot3", "woodbarefoot4", "woodbarefoot5"}},
		{BareFootstep.sand,
			 new List<string> {"asteroid1","asteroid2","asteroid3","asteroid4","asteroid5"}},
		{BareFootstep.water,
			 new List<string> {"water1", "water2", "water3", "water4"}},
		{BareFootstep.clownstep,
			 new List<string> {"clownstep1","clownstep2" }}
	};

	private readonly Dictionary<ClawFootstep, List<string>> ClawFootsteps = new Dictionary<ClawFootstep, List<string>>(){
		{ClawFootstep.floor,
			 new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}},
		{ClawFootstep.asteroid,
			 new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}},
		{ClawFootstep.carpet,
			 new List<string> {"carpetbarefoot1", "carpetbarefoot2", "carpetbarefoot3", "carpetbarefoot4", "carpetbarefoot5"}},
		{ClawFootstep.catwalk,
			 new List<string> {"catwalk1","catwalk2","catwalk3","catwalk4","catwalk5"}},
		{ClawFootstep.grass,
			 new List<string> {"grass1","grass2","grass3","grass4"}},
		{ClawFootstep.lava, //not literally
			 new List<string> {"lava1","lava2","lava3"}},
		{ClawFootstep.plating,
			new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}},
		{ClawFootstep.wood,
			 new List<string> {"woodclaw1", "woodclaw2", "woodclaw3", "woodclaw4", "woodclaw5"}},
		{ClawFootstep.sand,
			 new List<string> {"asteroid1","asteroid2","asteroid3","asteroid4","asteroid5"}},
		{ClawFootstep.water,
			 new List<string> {"water1", "water2", "water3", "water4"}}
	};

	private readonly Dictionary<HeavyFootstep, List<string>> HeavyFootsteps = new Dictionary<HeavyFootstep, List<string>>(){
		{HeavyFootstep.floor,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.asteroid,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.carpet,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.catwalk,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.grass,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.lava,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.plating,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.wood,
			 new List<string> {"suitstep1", "suitstep2"}},
		{HeavyFootstep.sand,
			 new List<string> {"lava1", "lava2", "lava3"}},
		{HeavyFootstep.water,
			 new List<string> {"water1", "water2", "water3", "water4"}}

	};

	private readonly Dictionary<ClownFoostep, List<string>> ClownFootsteps = new Dictionary<ClownFoostep, List<string>>(){
		{ClownFoostep.floor,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.asteroid,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.carpet,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.catwalk,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.grass,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.lava,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.plating,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.wood,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.sand,
			 new List<string> {"clownstep1", "clownstep2"}},
		{ClownFoostep.water,
			 new List<string> {"water1", "water2", "water3", "water4"}}

	};

	private static bool Step;
	private bool isMusicMute;

	private List<AudioSource> ambientTracks = new List<AudioSource>();
	public AudioSource ambientTrack;

	// Use this for initialization
	//public AudioSource[] sounds;
	public List<AudioSource> musicTracks = new List<AudioSource>();

	[SerializeField]
	private SongTracker songTracker;
	/// <summary>
	/// For controlling the song play list. Includes random shuffle and auto play
	/// </summary>
	public static SongTracker SongTracker => soundManager.songTracker;

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

	[SerializeField]
	private string[] RoundEndSounds = new string[]
	{
		"ApcDestroyed",
		"BanginDonk",
		"Disappointed",
		"ItsOnlyGame",
		"LeavingTG",
		"NewRoundSexy",
		"Scrunglartiy",
		"Yeehaw"
	};

	public AudioSource this[string key]
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
		//Mute Music Preference
		if (PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic))
		{
			isMusicMute = PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic) == 0;
		}

		//Ambient Volume Preference
		if (PlayerPrefs.HasKey(PlayerPrefKeys.AmbientVolumeKey))
		{
			AmbientVolume(PlayerPrefs.GetFloat(PlayerPrefKeys.AmbientVolumeKey));
		}
		else
		{
			AmbientVolume(1f);
		}
		layerMask = LayerMask.GetMask("Walls", "Door Closed");
		// Cache all sounds in the tree
		var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
		for (int i = 0; i < audioSources.Length; i++)
		{
			var audioSource = audioSources[i];
			if (audioSource.gameObject.CompareTag("AmbientSound"))
			{
				ambientTracks.Add(audioSource);
				continue;

			}
			if (audioSource.gameObject.CompareTag("Music"))
			{
				musicTracks.Add(audioSource);
				continue;
			}
			if (audioSource.gameObject.CompareTag("SoundFX"))
			{
				if (sounds.ContainsKey(audioSource.name))
				{
					Logger.LogErrorFormat("SoundManager: Duplicate sound name {0} on scene {1}, skipping!", Category.SoundFX,
						audioSource.name, SceneManager.GetActiveScene().name);
					continue;
				}
				sounds.Add(audioSource.name, audioSource);
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
		return soundPatterns[pattern] = sounds.Keys.Where((Func<string, bool>)regex.IsMatch).ToArray();
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
	public static void PlayNetworkedAtPos(string sndName, Vector3 worldPos, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, bool Global = true)
	{
		sndName = Instance.ResolveSoundPattern(sndName);
		if (Global)
		{
			PlaySoundMessage.SendToAll(sndName, worldPos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
		}
		else {
			PlaySoundMessage.SendToNearbyPlayers(sndName, worldPos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
		}
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
	public static void PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos, string sndName, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30)
	{
		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.Send(recipient, sndName, worldPos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
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
		Play(name, oneShot);
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
	public static void Play(string name, bool polyphonic = false, bool Global = true)
	{
		name = Instance.ResolveSoundPattern(name);
		var sound = Instance.sounds[name];

		if (!Global
			&& PlayerManager.LocalPlayer != null
			&& Physics2D.Linecast(PlayerManager.LocalPlayer.TileWorldPosition(), sound.transform.position, layerMask))
		{
			//Logger.Log("MuffledMixer");
			sound.outputAudioMixerGroup = soundManager.MuffledMixer;
		}
		else {
			sound.outputAudioMixerGroup = soundManager.DefaultMixer;
		}
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
	/// Play Footstep at given world position.
	/// </summary>

	//TODO creature has claws

	// Creature is barefoot (with humanlike foot)
	private static void BarefootAtPosition(Vector3 worldPos, BasicTile tile)
	{
		var WalkingSoundCategory = tile.BarefootWalkingSoundCategory;
		PlayNetworkedAtPos(Instance.BareFootsteps[WalkingSoundCategory][RANDOM.Next(Instance.BareFootsteps[WalkingSoundCategory].Count)],
									worldPos, (float)Instance.GetRandomNumber(0.7d, 1.2d),
									Global: false, polyphonic: true);
	}

	// TODO Creature is wearing hardsuit

	// Creature is wearing clown shoes
	private static void ClownStepAtPos(Vector3 worldPos, BasicTile tile)
	{
		var WalkingSoundCategory = tile.ClownFootstepSoundCategory;
		PlayNetworkedAtPos(Instance.ClownFootsteps[WalkingSoundCategory][RANDOM.Next(Instance.ClownFootsteps[WalkingSoundCategory].Count)],
									worldPos, (float)Instance.GetRandomNumber(0.7d, 1.2d),
									Global: false, polyphonic: true);
	}

	// Normal footsteps
	public static void FootstepAtPosition(Vector3 worldPos, Pickupable feetSlot)
	{
		MatrixInfo matrix = MatrixManager.AtPoint(worldPos.NormalizeToInt(), false);

		var locPos = matrix.ObjectParent.transform.InverseTransformPoint(worldPos).RoundToInt();
		var tile = matrix.MetaTileMap.GetTile(locPos) as BasicTile;

		if (tile != null)
		{
			if (Step)
			{
				if (feetSlot == null) 
				{
					//TODO when we have creatures with claws, check here to make proper claw sound
					BarefootAtPosition(worldPos, tile);
				}
				else if (Validations.HasItemTrait(feetSlot.gameObject, CommonTraits.Instance.Squeaky))
				{
					ClownStepAtPos(worldPos, tile);
				}
				else
				{
					PlayNetworkedAtPos(Instance.FootSteps[tile.WalkingSoundCategory][RANDOM.Next(Instance.FootSteps[tile.WalkingSoundCategory].Count)],
									worldPos, (float)Instance.GetRandomNumber(0.7d, 1.2d),
									Global: false, polyphonic: true);
				}
			}
			Step = !Step;
		}
	}


	/// <summary>
	/// Play Glassknock at given world position.
	/// </summary>
	public static void GlassknockAtPosition(Vector3 worldPos)
	{
		PlayNetworkedAtPos("GlassKnock", worldPos, (float)Instance.GetRandomNumber(0.7d, 1.2d),
						   Global: false, polyphonic: true);
	}


	/// <summary>
	/// Play sound locally at given world position.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayAtPosition(string name, Vector3 worldPos, float pitch = -1, bool polyphonic = false)
	{
		name = Instance.ResolveSoundPattern(name);
		if (Instance.sounds.ContainsKey(name))
		{
			var sound = Instance.sounds[name];
			if (pitch > 0)
			{
				sound.pitch = pitch;
			}
			sound.transform.position = worldPos;
			Play(name, polyphonic, false);
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

	/// <summary>
	/// Plays a random music track.
	/// Using two diiferent ways to play tracks, some tracks are normal audio and some are tracker files played by sunvox.
	/// <returns>String[] that represents the picked song's name.</returns>
	/// </summary>
	public static String[] PlayRandomTrack()
	{
		StopMusic();
		String[] songInfo;

		// To make sure not to play the last song that just played,
		// every time a track is played, it's either a normal audio or track played by sunvox, alternatively.
		if (currentLobbyAudioSource == null)
		{
			//Traditional music
			int randTrack = Random.Range(0, Instance.musicTracks.Count);
			currentLobbyAudioSource = Instance.musicTracks[randTrack];
			var volume = Instance.MusicVolume;
			if (Instance.isMusicMute)
			{
				volume = 0f;
			}
			currentLobbyAudioSource.volume = volume;
			currentLobbyAudioSource.Play();
			songInfo = currentLobbyAudioSource.clip.name.Split('_'); // Spliting to get the song and artist name
		}
		else
		{
			currentLobbyAudioSource = null;
			//Tracker music
			var trackerMusic = new[]
			{
				"Spaceman_HERB.xm",
				"Echo sound_4mat.xm",
				"Tintin on the Moon_Jeroen Tel.xm"
			};
			var songPicked = trackerMusic.Wrap(Random.Range(1, 100));
			var vol = 255 * Instance.MusicVolume;

			if (Instance.isMusicMute)
			{
				vol = 0f;
			}

			Synth.Instance.PlayMusic(songPicked, false, (byte)(int)vol);
			songPicked = songPicked.Split('.')[0]; // Throwing away the .xm extension in the string
			songInfo = songPicked.Split('_'); // Spliting to get the song and artist name
		}
		return songInfo;
	}

	public void ToggleMusicMute(bool mute)
	{
		isMusicMute = mute;
		foreach (var m in musicTracks)
		{
			m.mute = mute;
		}

		if (mute)
		{
			Synth.Instance.SetMusicVolume(Byte.MinValue);
		}
		else
		{
			var vol = 255 * Instance.MusicVolume;
			Synth.Instance.SetMusicVolume((byte) (int) vol);
		}
	}

	public static void PlayAmbience(string ambientTrackName)
	{
		void PlayAmbientTrack(AudioSource track)
		{
			Logger.Log($"Playing ambient track: {track.name}", Category.SoundFX);
			Instance.ambientTrack = track;
			//Ambient Volume
			if (PlayerPrefs.HasKey("AmbientVol"))
			{
				track.volume = Mathf.Clamp(PlayerPrefs.GetFloat("AmbientVol"),0f,0.25f);
			}
			track.Play();
		}

		foreach (var track in Instance.ambientTracks)
		{
			if (track.name == ambientTrackName)
			{
				PlayAmbientTrack(track);
			}
			else
			{
				track.Stop();
			}
		}
	}

	/// <summary>
	/// Sets all ambient tracks to a certain volume
	/// </summary>
	/// <param name="volume"></param>
	public static void AmbientVolume(float volume)
	{
		volume = Mathf.Clamp(volume, 0f, 0.25f);
		foreach (AudioSource s in Instance.ambientTracks)
		{
			s.volume = volume;
		}

		PlayerPrefs.SetFloat(PlayerPrefKeys.AmbientVolumeKey, volume);
		PlayerPrefs.Save();
	}

	/// <summary>
	/// Checks if music in lobby is being played or not.
	/// <returns> true if music is being played.</returns>
	/// </summary>
	public static bool isLobbyMusicPlaying()
    {
		// Checks if an audiosource or a track by sunvox is being played(Since there are two diiferent ways to play tracks)
		if (currentLobbyAudioSource != null && currentLobbyAudioSource.isPlaying || !(SunVox.sv_end_of_song((int)Slot.Music) == 1))
			return true;

		return false;
	}

	public double GetRandomNumber(double minimum, double maximum)
	{
		return  RANDOM.NextDouble() * (maximum - minimum) + minimum;
	}

	/// <summary>
	/// Plays a random round end sound using sounds picked from RoundEndSounds
	/// </summary>
	public void PlayRandomRoundEndSound()
	{
		var rand = RANDOM.Next(RoundEndSounds.Length);
		PlayNetworked(RoundEndSounds[rand], 1f);
	}

}

public enum Footstep
{
	floor,
	asteroid,
	carpet,
	catwalk,
	grass,
	lava,
	plating,
	wood,
	sand,
	water,
	clownstep
}
public enum BareFootstep
{
	floor = Footstep.floor,
	asteroid = Footstep.asteroid,
	carpet = Footstep.carpet,
	catwalk = Footstep.catwalk,
	grass = Footstep.grass,
	lava = Footstep.lava,
	plating = Footstep.plating,
	wood = Footstep.wood,
	sand = Footstep.sand,
	water = Footstep.water,
	clownstep = Footstep.clownstep
}

public enum HeavyFootstep
{
	floor = Footstep.floor,
	asteroid = Footstep.asteroid,
	carpet = Footstep.carpet,
	catwalk = Footstep.catwalk,
	grass = Footstep.grass,
	lava = Footstep.lava,
	plating = Footstep.plating,
	wood = Footstep.wood,
	sand = Footstep.sand,
	water = Footstep.water,
	clownstep = Footstep.clownstep
}

public enum ClawFootstep
{
	floor = Footstep.floor,
	asteroid = Footstep.asteroid,
	carpet = Footstep.carpet,
	catwalk = Footstep.catwalk,
	grass = Footstep.grass,
	lava = Footstep.lava,
	plating = Footstep.plating,
	wood = Footstep.wood,
	sand = Footstep.sand,
	water = Footstep.water,
	clownstep = Footstep.clownstep
}

public enum ClownFoostep 
{
	floor = Footstep.floor,
	asteroid = Footstep.asteroid,
	carpet = Footstep.carpet,
	catwalk = Footstep.catwalk,
	grass = Footstep.grass,
	lava = Footstep.lava,
	plating = Footstep.plating,
	wood = Footstep.wood,
	sand = Footstep.sand,
	water = Footstep.water,
}