using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Assets.Scripts.Messages.Server.SoundMessages;

public class SoundManager : MonoBehaviour
{
	public AudioMixerGroup DefaultMixer;

	public AudioMixerGroup MuffledMixer;

	private static LayerMask layerMask;

	private static SoundManager soundManager;

	public readonly Dictionary<string, AudioSource> sounds = new Dictionary<string, AudioSource>();

	private readonly Dictionary<string, string[]> soundPatterns = new Dictionary<string, string[]>();

	private static readonly System.Random RANDOM = new System.Random();

	private static bool Step;

	[SerializeField] private GameObject soundSpawnPrefab = null;
	private List<SoundSpawn> pooledSources = new List<SoundSpawn>();

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

	[SerializeField] private string[] RoundEndSounds = new string[]
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

	private void Awake()
	{
		Init();
	}

	private void Init()
	{
		//Master Volume
		if (PlayerPrefs.HasKey(PlayerPrefKeys.MasterVolumeKey))
		{
			MasterVolume(PlayerPrefs.GetFloat(PlayerPrefKeys.MasterVolumeKey));
		}
		else
		{
			MasterVolume(1f);
		}

		layerMask = LayerMask.GetMask("Door Closed");
		// Cache all sounds in the tree
		var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);
		for (int i = 0; i < audioSources.Length; i++)
		{
			var audioSource = audioSources[i];

			if (audioSource.gameObject.CompareTag("SoundFX"))
			{
				if (sounds.ContainsKey(audioSource.name))
				{
					Logger.LogErrorFormat("SoundManager: Duplicate sound name {0} on scene {1}, skipping!",
						Category.SoundFX,
						audioSource.name, SceneManager.GetActiveScene().name);
					continue;
				}

				sounds.Add(audioSource.name, audioSource);
			}
		}
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
	}

	private void OnSceneChange(Scene oldScene, Scene newScene)
	{
		ReinitSoundPool();
	}

	private void ReinitSoundPool()
	{
		for (int i = Instance.pooledSources.Count - 1; i > 0; i--)
		{
			if (Instance.pooledSources[i] != null)
			{
				Destroy(Instance.pooledSources[i].gameObject);
			}
		}

		Instance.pooledSources.Clear();

		// Cache some pooled sources:
		for (int i = 0; i < 20; i++)
		{
			var soundObj = Instantiate(soundSpawnPrefab, transform);
			pooledSources.Add(soundObj.GetComponent<SoundSpawn>());
		}
	}

	/// <summary>
	/// Uses a pooled AudioSource instead of the original one.
	/// This copies the sourceToCopy settings to a source taken from the pool
	/// and return it.
	/// </summary>
	private SoundSpawn GetSourceFromPool(AudioSource sourceToCopy)
	{
		for (int i = pooledSources.Count - 1; i > 0; i--)
		{
			if (pooledSources[i] != null && pooledSources[i].gameObject != null
			                             && !pooledSources[i].isPlaying)
			{
				pooledSources[i].isPlaying = true;
				CopySource(pooledSources[i].audioSource, sourceToCopy);
				return pooledSources[i];
			}
		}

		var soundObj = Instantiate(soundSpawnPrefab, transform);
		var source = soundObj.GetComponent<SoundSpawn>();
		pooledSources.Add(source);
		source.isPlaying = true;
		CopySource(source.audioSource, sourceToCopy);
		return source;
	}

	private void CopySource(AudioSource newSource, AudioSource sourceToCopy)
	{
		newSource.clip = sourceToCopy.clip;
		newSource.loop = sourceToCopy.loop;
		newSource.pitch = sourceToCopy.pitch;
		newSource.mute = sourceToCopy.mute;
		newSource.spatialize = sourceToCopy.spatialize;
		newSource.spread = sourceToCopy.spread;
		newSource.volume = sourceToCopy.volume;
		newSource.bypassEffects = sourceToCopy.bypassEffects;
		newSource.dopplerLevel = sourceToCopy.dopplerLevel;
		newSource.maxDistance = sourceToCopy.maxDistance;
		newSource.minDistance = sourceToCopy.minDistance;
		newSource.panStereo = sourceToCopy.panStereo;
		newSource.rolloffMode = sourceToCopy.rolloffMode;
		newSource.spatialBlend = sourceToCopy.spatialBlend;
		newSource.bypassListenerEffects = sourceToCopy.bypassListenerEffects;
		newSource.bypassReverbZones = sourceToCopy.bypassReverbZones;
		newSource.reverbZoneMix = sourceToCopy.reverbZoneMix;
		newSource.spatializePostEffects = sourceToCopy.spatializePostEffects;
		newSource.outputAudioMixerGroup = sourceToCopy.outputAudioMixerGroup;
		newSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
			sourceToCopy.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
		newSource.SetCustomCurve(AudioSourceCurveType.Spread,
			sourceToCopy.GetCustomCurve(AudioSourceCurveType.Spread));
		newSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend,
			sourceToCopy.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
		newSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix,
			sourceToCopy.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
	}

	/// <summary>
	/// Chooses a random sound matching the given pattern if the name contains a wildcard. (#)
	/// Otherwise, it returns the same name.
	/// </summary>
	private string ResolveSoundPattern(string sndName)
	{
		if (sndName == null) return "";

		if (sounds.ContainsKey(sndName) || !sndName.Contains('#')) return sndName;
		var soundNames = GetMatchingSounds(sndName);
		if (soundNames.Length > 0)
		{
			return soundNames[Random.Range(0, soundNames.Length)];
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
		ShakeParameters shakeParameters = null;
		if (shakeGround == true)
		{
			shakeParameters = new ShakeParameters
			{
				ShakeGround = shakeGround,
				ShakeIntensity = shakeIntensity,
				ShakeRange = shakeRange
			};
		}

		AudioSourceParameters audioSourceParameters = null;
		if (pitch > 0)
		{
			audioSourceParameters = new AudioSourceParameters
			{
				Pitch = pitch
			};
		}

		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.SendToAll(sndName, TransformState.HiddenPos, polyphonic, null, shakeParameters,
			audioSourceParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	/// <param name="sndName">The name of the sound to be played</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="Global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	public static void PlayNetworkedAtPos(string sndName, Vector3 worldPos, AudioSourceParameters audioSourceParameters,
		bool polyphonic = false, bool Global = true, GameObject sourceObj = null,
		ShakeParameters shakeParameters = null)
	{
		sndName = Instance.ResolveSoundPattern(sndName);
		if (Global)
		{
			PlaySoundMessage.SendToAll(sndName, worldPos, polyphonic, sourceObj, shakeParameters,
				audioSourceParameters);
		}
		else
		{
			PlaySoundMessage.SendToNearbyPlayers(sndName, worldPos, polyphonic, sourceObj, shakeParameters,
				audioSourceParameters);
		}
	}


	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayNetworkedAtPos(string sndName, Vector3 worldPos, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, bool global = true,
		GameObject sourceObj = null)
	{
		ShakeParameters shakeParameters = null;
		if (shakeGround == true)
		{
			shakeParameters = new ShakeParameters
			{
				ShakeGround = shakeGround,
				ShakeIntensity = shakeIntensity,
				ShakeRange = shakeRange
			};
		}

		AudioSourceParameters audioSourceParameters = null;
		if (pitch > 0)
		{
			audioSourceParameters = new AudioSourceParameters
			{
				Pitch = pitch
			};
		}

		PlayNetworkedAtPos(sndName, worldPos, audioSourceParameters, polyphonic, global, sourceObj, shakeParameters);
	}

	/// <summary>
	/// Serverside: Play sound for particular player.
	/// ("Doctor, there are voices in my head!")
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayNetworkedForPlayer(GameObject recipient, string sndName, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, GameObject sourceObj = null)
	{
		ShakeParameters shakeParameters = null;
		if (shakeGround == true)
		{
			shakeParameters = new ShakeParameters
			{
				ShakeGround = shakeGround,
				ShakeIntensity = shakeIntensity,
				ShakeRange = shakeRange
			};
		}

		AudioSourceParameters audioSourceParameters = null;
		if (pitch > 0)
		{
			audioSourceParameters = new AudioSourceParameters
			{
				Pitch = pitch
			};
		}

		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.Send(recipient, sndName, TransformState.HiddenPos, polyphonic, sourceObj, shakeParameters,
			audioSourceParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for particular player.
	/// ("Doctor, there are voices in my head!")
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos, string sndName,
		float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, GameObject sourceObj = null)
	{
		ShakeParameters shakeParameters = null;
		if (shakeGround)
		{
			shakeParameters = new ShakeParameters
			{
				ShakeGround = shakeGround,
				ShakeIntensity = shakeIntensity,
				ShakeRange = shakeRange
			};
		}

		AudioSourceParameters audioSourceParameters = null;
		if (pitch > 0)
		{
			audioSourceParameters = new AudioSourceParameters
			{
				Pitch = pitch
			};
		}

		sndName = Instance.ResolveSoundPattern(sndName);
		PlaySoundMessage.Send(recipient, sndName, worldPos, polyphonic, sourceObj, shakeParameters,
			audioSourceParameters);
	}

	/// <summary>
	/// Play a sound locally
	/// </summary>
	/// <param name="name">Name of the sound to be played</param>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	public static void Play(string name, AudioSourceParameters audioSourceParameters, bool polyphonic = false)
	{
		name = Instance.ResolveSoundPattern(name);
		var sound = Instance.GetSourceFromPool(Instance.sounds[name]);

		ApplyAudioSourceParameters(audioSourceParameters, sound.audioSource);

		Instance.PlaySource(sound, polyphonic, true,
			audioSourceParameters != null && audioSourceParameters.MixerType != MixerType.Unspecified);
	}

	/// <summary>
	/// Play sound locally.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void Play(string name, float volume, float pitch = -1, float time = 0, bool oneShot = false,
		float pan = 0)
	{
		name = Instance.ResolveSoundPattern(name);
		var sound = Instance.GetSourceFromPool(Instance.sounds[name]);
		if (pitch > 0)
		{
			sound.audioSource.pitch = pitch;
		}

		sound.audioSource.time = time;
		sound.audioSource.volume = volume;
		sound.audioSource.panStereo = pan;
		Instance.PlaySource(sound, oneShot);
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
	/// <param name="name">Name of the sound to be played</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	/// <param name="global">Should the sound be played for the default mixer or false to check if it should play muffled</param>
	/// <remarks>
	///		If Global is true, the sound may still be muffled if the source is configured with the muffled mixer.
	/// </remarks>
	public static void Play(string name, bool polyphonic = false, bool global = true)
	{
		name = Instance.ResolveSoundPattern(name);
		Instance.PlaySource(Instance.GetSourceFromPool(Instance.sounds[name]), polyphonic, global);
	}

	private void PlaySource(SoundSpawn source, bool polyphonic = false, bool Global = true, bool forceMixer = false)
	{
		if (!forceMixer)
		{
			if (!Global
			    && PlayerManager.LocalPlayer != null)

			{
				if (((Vector3)(Vector2) PlayerManager.LocalPlayer.TileWorldPosition() - source.transform.position).magnitude <
				    15f)
				{
					if (MatrixManager.Linecast((Vector2) PlayerManager.LocalPlayer.TileWorldPosition(),
						LayerTypeSelection.Walls, layerMask, source.transform.position).ItHit)
					{
						//Logger.Log("MuffledMixer");
						source.audioSource.outputAudioMixerGroup = soundManager.MuffledMixer;
					}
				}
				else
				{
					source.audioSource.outputAudioMixerGroup = soundManager.MuffledMixer;
				}
			}
		}

		if (polyphonic)
		{
			source.PlayOneShot();
		}
		else
		{
			source.PlayNormally();
		}
	}

	/// <summary>
	/// Play Glassknock at given world position.
	/// </summary>
	public static void GlassknockAtPosition(Vector3 worldPos, GameObject performer = null)
	{
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters
		{
			Pitch = (float) Instance.GetRandomNumber(0.7d, 1.2d)
		};

		PlayNetworkedAtPos("GlassKnock", worldPos, audioSourceParameters, true, false, performer);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// This static method is for specifically attaching sound play to a target object (it will
	/// parent itself to the target and set its local position to Vector3.zero before playing)
	/// This is useful for moving objects that play sounds
	/// </summary>
	public static void PlayAtPosition(string name, Vector3 worldPos, GameObject sourceObj,
		bool polyphonic = false,
		bool isGlobal = false,
		AudioSourceParameters audioSourceParameters = null)
	{
		var netId = NetId.Empty;
		if (sourceObj != null)
		{
			var netB = sourceObj.GetComponent<NetworkBehaviour>();
			if (netB != null)
			{
				netId = netB.netId;
			}
		}

		PlayAtPosition(name, worldPos, polyphonic, isGlobal, netId, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void PlayAtPosition(string name, Vector3 worldPos, bool polyphonic = false,
		bool isGlobal = false, uint netId = NetId.Empty, AudioSourceParameters audioSourceParameters = null)
	{
		name = Instance.ResolveSoundPattern(name);
		if (!Instance.sounds.ContainsKey(name)) return;
		var sound = Instance.GetSourceFromPool(Instance.sounds[name]);

		ApplyAudioSourceParameters(audioSourceParameters, sound.audioSource);

		if (netId != NetId.Empty)
		{
			if (NetworkIdentity.spawned.ContainsKey(netId))
			{
				sound.transform.parent = NetworkIdentity.spawned[netId].transform;
				sound.transform.localPosition = Vector3.zero;
			}
			else
			{
				sound.transform.parent = Instance.transform;
				sound.transform.position = worldPos;
			}
		}
		else
		{
			sound.transform.parent = Instance.transform;
			sound.transform.position = worldPos;
		}

		Instance.PlaySource(sound, polyphonic, isGlobal,
			audioSourceParameters != null && audioSourceParameters.MixerType != MixerType.Unspecified);
	}

	private static void ApplyAudioSourceParameters(AudioSourceParameters audioSourceParameters, AudioSource audioSource)
	{
		if (audioSourceParameters != null)
		{
			if (audioSourceParameters.MixerType != MixerType.Unspecified)
				audioSource.outputAudioMixerGroup = audioSourceParameters.MixerType == MixerType.Master
					? Instance.DefaultMixer
					: Instance.MuffledMixer;

			if (audioSourceParameters.Pitch != null)
				audioSource.pitch = audioSourceParameters.Pitch.Value;
			else
				audioSource.pitch = 1;

			if (audioSourceParameters.Time != null)
				audioSource.time = audioSourceParameters.Time.Value;

			if (audioSourceParameters.Volume != null)
				audioSource.volume = audioSourceParameters.Volume.Value;

			if (audioSourceParameters.Pan != null)
				audioSource.panStereo = audioSourceParameters.Pan.Value;

			if (audioSourceParameters.SpatialBlend != null)
				audioSource.spatialBlend = audioSourceParameters.SpatialBlend.Value;

			if (audioSourceParameters.MinDistance != null)
				audioSource.minDistance = audioSourceParameters.MinDistance.Value;

			if (audioSourceParameters.MaxDistance != null)
				audioSource.maxDistance = audioSourceParameters.MaxDistance.Value;

			if (audioSourceParameters.Spread != null)
				audioSource.spread = audioSourceParameters.Spread.Value;

			switch (audioSourceParameters.VolumeRolloffType)
			{
				case VolumeRolloffType.EaseInAndOut:
					audioSource.rolloffMode = AudioRolloffMode.Custom;
					audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
						AnimationCurve.EaseInOut(0, 1, 1, 0));
					break;
				case VolumeRolloffType.Linear:
					audioSource.rolloffMode = AudioRolloffMode.Linear;
					break;
				case VolumeRolloffType.Logarithmic:
					audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
					break;
			}
		}
	}

	/// <summary>
	/// Tell all clients to stop playing a sound
	/// </summary>
	/// <param name="name">The sound to be stopped</param>
	public static void StopNetworked(string name)
	{
		StopSoundMessage.SendToAll(name);
	}

	/// <summary>
	/// Stops a given sound from playing locally.
	/// Accepts "#" wildcards for sound variations. (Example: "Punch#")
	/// </summary>
	public static void Stop(string name)
	{
		if (Instance.sounds.ContainsKey(name))
		{
			var sound = Instance.sounds[name];

			for (int i = Instance.pooledSources.Count - 1; i > 0; i--)
			{
				if (Instance.pooledSources[i] == null) continue;

				if (Instance.pooledSources[i].isPlaying && Instance.pooledSources[i].audioSource.clip == sound.clip)
				{
					Instance.pooledSources[i].audioSource.Stop();
				}
			}

			sound.Stop();
		}
		else
		{
			foreach (var sound in Instance.GetMatchingSounds(name))
			{
				var s = Instance.sounds[sound];
				for (int i = Instance.pooledSources.Count - 1; i > 0; i--)
				{
					if (Instance.pooledSources[i] == null) continue;
					if (Instance.pooledSources[i].isPlaying && Instance.pooledSources[i].audioSource.clip == s.clip)
					{
						Instance.pooledSources[i].audioSource.Stop();
					}
				}

				s.Stop();
			}
		}
	}

	/// <summary>
	/// Sets all Sounds volume
	/// </summary>
	/// <param name="volume"></param>
	public static void MasterVolume(float volume)
	{
		AudioListener.volume = volume;
		PlayerPrefs.SetFloat(PlayerPrefKeys.MasterVolumeKey, volume);
		PlayerPrefs.Save();
	}

	/// <summary>
	/// Changes the Audio Source Parameters of a sound
	/// </summary>
	/// <param name="soundName">The name of the sound to change the mixer</param>
	/// <param name="audioSourceParameters">The Audio Source Parameters to apply</param>
	public static void ChangeAudioSourceParameters(string soundName, AudioSourceParameters audioSourceParameters)
	{
		if (Instance.sounds.ContainsKey(soundName))
		{
			AudioSource sound = Instance.sounds[soundName];

			for (int i = Instance.pooledSources.Count - 1; i > 0; i--)
			{
				if (Instance.pooledSources[i] == null) continue;

				if (Instance.pooledSources[i].isPlaying && Instance.pooledSources[i].audioSource.clip == sound.clip)
				{
					ApplyAudioSourceParameters(audioSourceParameters, Instance.pooledSources[i].audioSource);
				}
			}

			if (sound != null)
				ApplyAudioSourceParameters(audioSourceParameters, sound);
		}
	}

	public double GetRandomNumber(double minimum, double maximum)
	{
		return RANDOM.NextDouble() * (maximum - minimum) + minimum;
	}

	/// <summary>
	/// Plays a random round end sound using sounds picked from RoundEndSounds
	/// </summary>
	public void PlayRandomRoundEndSound()
	{
		var rand = RANDOM.Next(RoundEndSounds.Length);
		PlayNetworked(RoundEndSounds[rand], 1f);
	}

	//TODO Please someone who knows what he's doing, take all of this outside this class!
	private static bool step;

	/// <summary>
	/// Play footsteps at given position. It will handle all the logic to determine
	/// the proper sound to use.
	/// </summary>
	/// <param name="worldPos">Where in the world is this sound coming from. Also used to get the type of tile</param>
	/// <param name="stepType">What kind of step does the creature walking have</param>
	/// <param name="performer">The creature making the sound</param>
	public static void FootstepAtPosition(Vector3 worldPos, StepType stepType, GameObject performer)
	{
		MatrixInfo matrix = MatrixManager.AtPoint(worldPos.NormalizeToInt(), false);
		var locPos = matrix.ObjectParent.transform.InverseTransformPoint(worldPos).RoundToInt();
		var tile = matrix.MetaTileMap.GetTile(locPos) as BasicTile;

		if (tile != null)
		{
			if (step)
			{
				AudioSourceParameters audioSourceParameters = new AudioSourceParameters
				{
					Pitch = Random.Range(0.7f, 1.2f)
				};

				PlayNetworkedAtPos(
					Instance.stepSounds[stepType][tile.floorTileType].PickRandom(),
					worldPos,
					audioSourceParameters,
					polyphonic: true,
					Global: false,
					sourceObj: performer
				);
			}

			step = !step;
		}
	}

	private readonly Dictionary<StepType, Dictionary<FloorTileType, List<string>>> stepSounds =
		new Dictionary<StepType, Dictionary<FloorTileType, List<string>>>()
		{
			{
				StepType.Barefoot,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string>
							{"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}
					},
					{
						FloorTileType.asteroid,
						new List<string>
							{"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}
					},
					{
						FloorTileType.carpet,
						new List<string>
						{
							"carpetbarefoot1", "carpetbarefoot2", "carpetbarefoot3", "carpetbarefoot4",
							"carpetbarefoot5"
						}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"catwalk1", "catwalk2", "catwalk3", "catwalk4", "catwalk5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"grass1", "grass2", "grass3", "grass4"}
					},
					{
						FloorTileType.lava,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.plating,
						new List<string>
							{"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}
					},
					{
						FloorTileType.wood,
						new List<string>
							{"woodbarefoot1", "woodbarefoot2", "woodbarefoot3", "woodbarefoot4", "woodbarefoot5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					}
				}
			},
			{
				StepType.Claw,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}
					},
					{
						FloorTileType.carpet,
						new List<string>
						{
							"carpetbarefoot1", "carpetbarefoot2", "carpetbarefoot3", "carpetbarefoot4",
							"carpetbarefoot5"
						}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"catwalk1", "catwalk2", "catwalk3", "catwalk4", "catwalk5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"grass1", "grass2", "grass3", "grass4"}
					},
					{
						FloorTileType.lava,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.plating,
						new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}
					},
					{
						FloorTileType.wood,
						new List<string> {"woodclaw1", "woodclaw2", "woodclaw3", "woodclaw4", "woodclaw5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					}
				}
			},
			{
				StepType.Shoes,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"floor1", "floor2", "floor3", "floor4", "floor5"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"carpet1", "carpet2", "carpet3", "carpet4", "carpet5"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"catwalk1", "catwalk2", "catwalk3", "catwalk4", "catwalk5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"grass1", "grass2", "grass3", "grass4"}
					},
					{
						FloorTileType.lava,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.plating,
						new List<string> {"plating1", "plating2", "plating3", "plating4", "plating5"}
					},
					{
						FloorTileType.wood,
						new List<string> {"wood1", "wood2", "wood3", "wood4", "wood5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					},
				}
			},
			{
				StepType.Suit,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.lava,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.plating,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.wood,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					},
				}
			},
			{
				StepType.Heavy,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.grass,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.lava,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.plating,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.wood,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.sand,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					},
				}
			},
			{
				StepType.Clown,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.grass,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.lava,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.plating,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.wood,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.sand,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					}
				}
			}
		};
}