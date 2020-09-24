using AddressableReferences;
using Assets.Scripts.Messages.Server.SoundMessages;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager that allows to play sounds.
/// Should they be local (single client) or networked across one or more client.
/// </summary>
public class SoundManager : MonoBehaviour
{
	public AudioMixerGroup DefaultMixer;

	public AudioMixerGroup MuffledMixer;

	private static LayerMask layerMask;

	private static SoundManager soundManager;

	[SerializeField]
	private GameObject soundSpawnPrefab = null;

	/// <summary>
	/// Library of AddressableAudioSource
	/// </summary>
	[HideInInspector]
	public readonly List<AddressableAudioSource> SoundsLibrary = new List<AddressableAudioSource>();

	/// <summary>
	/// Library of music paths (primaryKey) and their AudioSource if loaded
	/// </summary>
	/// <remarks>
	/// If AudioSource is null, it means it's not loaded.
	/// </remarks>
	[HideInInspector]
	public readonly List<AddressableAudioSource> MusicLibrary = new List<AddressableAudioSource>();

	/// <summary>
	/// A list of all sounds currently playing
	/// </summary>
	/// <remarks>
	/// Thats useful for interrupting playing sounds, and preventing a sound to play over itself.
	/// Key is a Guid representing the token of the current playing sound.
	/// </remarks>
	private Dictionary<string, SoundSpawn> SoundSpawns = new Dictionary<string, SoundSpawn>();

	/// <summary>
	/// Load the AudioSource of a music inside the library and returns it.
	/// </summary>
	/// <param name="primaryKey">The primary key (path) of the music to load</param>
	/// <returns>The AudioSource component of the music</returns>
	public async Task<AddressableAudioSource> LoadMusicAsync(string primaryKey)
	{
		AddressableAudioSource addressableAudioSource = MusicLibrary.FirstOrDefault(p => p.Path == primaryKey);

		if (addressableAudioSource == null)
			throw new ArgumentException($"Invalid music path {primaryKey}");

		await addressableAudioSource.Load();

		return addressableAudioSource;
	}

	/// <summary>
	/// Unload the music by it's primaryKey (path), freeing resource RAM usage
	/// </summary>
	/// <param name="primaryKey">The primary key (path) of the music to unload </param>
	public void UnloadMusic(string primaryKey)
	{
		AddressableAudioSource addressableAudioSource = MusicLibrary.FirstOrDefault(p => p.Path == primaryKey);

		if (addressableAudioSource == null)
			throw new ArgumentException($"Invalid music path {primaryKey}");

		addressableAudioSource.Unload();
    }

	/// <summary>
	/// Adds all musics to the music library.
	/// </summary>
	/// <remarks>
	/// Musics are identified in Addressable groups with a special label "Music"
	/// </remarks>
	private async void AddMusicsToLibraryAsync()
	{
        // We build the library of musics location (by a special Label that identifies them).
        IList<IResourceLocation> resourceLocations = await Addressables.LoadResourceLocationsAsync("Music", typeof(GameObject)).Task;

        foreach (IResourceLocation resourceLocation in resourceLocations)
        {
            MusicLibrary.Add(new AddressableAudioSource(resourceLocation.PrimaryKey));
        }
    }

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

	private void Awake()
	{
		Init();
	}

	private void Start()
	{
		// Load all musics in the music library
		AddMusicsToLibraryAsync();
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

		layerMask = LayerMask.GetMask("Walls", "Door Closed");
	}

	/// <summary>
	/// Add an addressable audio source to the common source pool.
	/// Caching it and loading it in RAM in the same process.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, a single one will be picked at random</param>
	/// <returns>The addressable audio source with it's component loaded</returns>
	private static async Task<AddressableAudioSource> AddAddressableAudioSourceToLibrary(List<AddressableAudioSource> addressableAudioSources)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		AddressableAudioSource addressableAudioSourceFromCache = Instance.SoundsLibrary.FirstOrDefault(p => p.AssetReference == addressableAudioSource.AssetReference);

		if (addressableAudioSourceFromCache == null)
		{
			AudioSource audioSource;
			GameObject gameObject = await addressableAudioSource.Load();

			if (!gameObject.TryGetComponent(out audioSource))
			{
				throw new ArgumentException($"AssetReference in SoundManager doesn't contain an AudioSource: {addressableAudioSource.AssetReference.SubObjectName}");
			}

			Instance.SoundsLibrary.Add(addressableAudioSource);
		}

		return addressableAudioSource;
	}

	private static async Task<AddressableAudioSource> GetAddressableAudioSourceFromLibrary(List<AddressableAudioSource> addressableAudioSources)
	{
		return await AddAddressableAudioSourceToLibrary(addressableAudioSources);
	}

	private static async Task<AudioSource> GetAudioSourceFromLibrary(List<AddressableAudioSource> addressableAudioSources)
	{
		AddressableAudioSource addressableAudioSource = await GetAddressableAudioSourceFromLibrary(addressableAudioSources);
		return addressableAudioSource.AudioSource;
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
		Instance.SoundSpawns.Clear();
	}

	/// <summary>
	/// Generates a SoundSpawn and put it the SoundSpawns list.
	/// This copies the AudioSource settings to the new SoundSpawn instance and returns it.
	/// </summary>
	/// <param name="audioSource">The AudioSource to copy</param>
	/// <returns>The SoundSpawn to be played</returns>
	private SoundSpawn GetNewSoundSpawn(AudioSource audioSource)
	{
		// The position doesn't matter at this point, but we need to provide one.
		GameObject soundSpawnObject = Instantiate(Instance.soundSpawnPrefab, Vector3.zero, Quaternion.identity);
		SoundSpawn soundSpawn = soundSpawnObject.GetComponent<SoundSpawn>();
		soundSpawn.SetAudioSource(audioSource);
		SoundSpawns.Add(soundSpawn.Token, soundSpawn);
		return soundSpawn;
	}

	/// <summary>
	/// Play sound for all clients.
	/// If more than one sound is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">List of sounds to be played.  If more than one sound is specified, one will be picked at random</param>
	public static async void PlayNetworked(List<AddressableAudioSource> addressableAudioSources, float pitch = -1,
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

		AddressableAudioSource addressableAudioSource = await GetAddressableAudioSourceFromLibrary(addressableAudioSources);
		PlaySoundMessage.SendToAll(addressableAudioSource, TransformState.HiddenPos, polyphonic, null, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// If more than one sound is specified, the sound will be chosen at random
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, a single one will be picked at random</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="Global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	public static async void PlayNetworkedAtPos(List<AddressableAudioSource> addressableAudioSources, Vector3 worldPos, AudioSourceParameters audioSourceParameters,
		bool polyphonic = false, bool Global = true, GameObject sourceObj = null, ShakeParameters shakeParameters = null)
	{
		AddressableAudioSource addressableAudioSource = await GetAddressableAudioSourceFromLibrary(addressableAudioSources);

		if (Global)
		{
			PlaySoundMessage.SendToAll(addressableAudioSource, worldPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
		}
		else
		{
			PlaySoundMessage.SendToNearbyPlayers(addressableAudioSource, worldPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
		}
	}


	/// <summary>
	/// Play sound at given position for all clients.
	/// </summary>
	/// If more than one is specified, one will be picked at random.
	/// <param name="addressableAudioSource">The sound to be played.</param>
	public static void PlayNetworkedAtPos(AddressableAudioSource addressableAudioSource, Vector3 worldPos, float pitch = -1,
		bool polyphonic = false, bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, bool global = true, GameObject sourceObj = null)
	{
		PlayNetworkedAtPos(new List<AddressableAudioSource>() { addressableAudioSource }, worldPos, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange, global, sourceObj);
	}

	/// <summary>
	/// Play sound at given position for all clients.
	/// </summary>
	/// If more than one is specified, one will be picked at random.
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static void PlayNetworkedAtPos(List<AddressableAudioSource> addressableAudioSources, Vector3 worldPos, float pitch = -1,
	bool polyphonic = false, bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, bool global = true, GameObject sourceObj = null)
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

		PlayNetworkedAtPos(addressableAudioSources, worldPos, audioSourceParameters, polyphonic, global, sourceObj, shakeParameters);
	}

	/// <summary>
	/// Play sound for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="recipient">The player that will receive the sound</param>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="pitch">The pitch variation of the sound.  Null for default pitch.</param>
	public static async void PlayNetworkedForPlayer(GameObject recipient, List<AddressableAudioSource> addressableAudioSources, float? pitch = null,
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
		if (pitch != null)
		{
			audioSourceParameters = new AudioSourceParameters
			{
				Pitch = pitch
			};
		}

		AddressableAudioSource addressableAudioSource = await GetAddressableAudioSourceFromLibrary(addressableAudioSources);
		PlaySoundMessage.Send(recipient, addressableAudioSource, TransformState.HiddenPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static async void PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos, List<AddressableAudioSource> addressableAudioSources,
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

		AddressableAudioSource addressableAudioSource = await GetAddressableAudioSourceFromLibrary(addressableAudioSources);
		PlaySoundMessage.Send(recipient, addressableAudioSource, worldPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Play a sound locally
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	public static async void Play(AddressableAudioSource addressableAudioSource, AudioSourceParameters audioSourceParameters, bool polyphonic = false)
	{
		Play(new List<AddressableAudioSource>() { addressableAudioSource }, audioSourceParameters, polyphonic);
	}

	/// <summary>
	/// Play a sound locally
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	public static async void Play(List<AddressableAudioSource> addressableAudioSources, AudioSourceParameters audioSourceParameters, bool polyphonic = false)
	{
		AudioSource audioSource = await GetAudioSourceFromLibrary(addressableAudioSources);
		SoundSpawn soundSpawn = Instance.GetNewSoundSpawn(audioSource);
		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);

		Instance.PlaySource(soundSpawn, polyphonic, true, audioSourceParameters != null && audioSourceParameters.MixerType != MixerType.Unspecified);
	}

	/// <summary>
	/// Play sound locally.
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static async void Play(List<AddressableAudioSource> addressableAudioSources, float volume, float pitch = -1, float time = 0, bool oneShot = false,
		float pan = 0)
	{
		AudioSource audioSource = await GetAudioSourceFromLibrary(addressableAudioSources);

		SoundSpawn soundSpawn = Instance.GetNewSoundSpawn(audioSource);

		if (pitch > 0)
		{
			soundSpawn.AudioSource.pitch = pitch;
		}

		soundSpawn.AudioSource.time = time;
		soundSpawn.AudioSource.volume = volume;
		soundSpawn.AudioSource.panStereo = pan;
		Instance.PlaySource(soundSpawn, oneShot);
	}


	/// <summary>
	/// Play sound locally.
	/// If more than one element is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	/// <param name="global">Should the sound be played for the default mixer or false to check if it should play muffled</param>
	/// <remarks>
	///		If Global is true, the sound may still be muffled if the source is configured with the muffled mixer.
	/// </remarks>
	public static async void Play(AddressableAudioSource addressableAudioSource, bool polyphonic = false, bool global = true)
	{
		Play(new List<AddressableAudioSource>() { addressableAudioSource }, polyphonic, global);
	}

	/// <summary>
	/// Play sound locally.
	/// If more than one element is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	/// <param name="global">Should the sound be played for the default mixer or false to check if it should play muffled</param>
	/// <remarks>
	///		If Global is true, the sound may still be muffled if the source is configured with the muffled mixer.
	/// </remarks>
	public static async void Play(List<AddressableAudioSource> addressableAudioSources, bool polyphonic = false, bool global = true)
	{
		AudioSource audioSource = await GetAudioSourceFromLibrary(addressableAudioSources);
		Instance.PlaySource(Instance.GetNewSoundSpawn(audioSource), polyphonic, global);
	}

	private void PlaySource(SoundSpawn source, bool polyphonic = false, bool Global = true, bool forceMixer = false)
	{
		if (!forceMixer)
		{
			if (!Global
				&& PlayerManager.LocalPlayer != null
				&& Physics2D.Linecast(PlayerManager.LocalPlayer.TileWorldPosition(), source.RegisterTile.WorldPositionClient.To2Int(), layerMask))
			{
				source.AudioSource.outputAudioMixerGroup = soundManager.MuffledMixer;
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

	public static void PlayAtPosition(AddressableAudioSource addressableAudioSource, Vector3 worldPos, GameObject sourceObj,
		bool polyphonic = false,
		bool isGlobal = false,
		AudioSourceParameters audioSourceParameters = null)
	{
		PlayAtPosition(new List<AddressableAudioSource>() { addressableAudioSource },
			worldPos, sourceObj, polyphonic, isGlobal, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// If more than one element is specified, one will be picked at random.
	/// This static method is for specifically attaching sound play to a target object (it will
	/// parent itself to the target and set its local position to Vector3.zero before playing)
	/// This is useful for moving objects that play sounds
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static void PlayAtPosition(List<AddressableAudioSource> addressableAudioSources, Vector3 worldPos, GameObject sourceObj,
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

		PlayAtPosition(addressableAudioSources, worldPos, polyphonic, isGlobal, netId, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.</param>
	public static async void PlayAtPosition(AddressableAudioSource addressableAudioSource, Vector3 worldPos, bool polyphonic = false,
		bool isGlobal = false, uint netId = NetId.Empty, AudioSourceParameters audioSourceParameters = null)
	{
		PlayAtPosition(new List<AddressableAudioSource>() { addressableAudioSource }, worldPos, polyphonic, isGlobal, netId, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// If more than one element is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static async void PlayAtPosition(List<AddressableAudioSource> addressableAudioSources, Vector3 worldPos, bool polyphonic = false,
		bool isGlobal = false, uint netId = NetId.Empty, AudioSourceParameters audioSourceParameters = null)
	{
		AudioSource audioSource = await GetAudioSourceFromLibrary(addressableAudioSources);
		SoundSpawn soundSpawn = Instance.GetNewSoundSpawn(audioSource);

		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);

		if (netId != NetId.Empty)
		{
			if (NetworkIdentity.spawned.ContainsKey(netId))
			{
				soundSpawn.transform.parent = NetworkIdentity.spawned[netId].transform;
				soundSpawn.transform.localPosition = Vector3.zero;
			}
			else
			{
				soundSpawn.transform.parent = Instance.transform;
				soundSpawn.transform.position = worldPos;
			}
		}
		else
		{
			soundSpawn.transform.parent = Instance.transform;
			soundSpawn.transform.position = worldPos;
		}

		Instance.PlaySource(soundSpawn, polyphonic, isGlobal, audioSourceParameters != null && audioSourceParameters.MixerType != MixerType.Unspecified);
	}

	private static void ApplyAudioSourceParameters(AudioSourceParameters audioSourceParameters, SoundSpawn soundSpawn)
	{
		AudioSource audioSource = soundSpawn.AudioSource;

		if (audioSourceParameters != null)
		{
			if (audioSourceParameters.MixerType != MixerType.Unspecified)
				audioSource.outputAudioMixerGroup = audioSourceParameters.MixerType == MixerType.Master ? Instance.DefaultMixer : Instance.MuffledMixer;

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
					audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0, 1, 1, 0));
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
	/// </summary>
	/// <param name="soundSpawnToken">The Token of the soundSpawn to stop</param>
	public static void Stop(string soundSpawnToken)
	{
		if (Instance.SoundSpawns.ContainsKey(soundSpawnToken))
			Instance.SoundSpawns[soundSpawnToken].AudioSource.Stop();

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
	/// Changes the Audio Source Parameters of a sound currently playing
	/// </summary>
	/// <param name="soundSpawnToken">The Token of the sound spawn to change the parameters</param>
	/// <param name="audioSourceParameters">The Audio Source Parameters to apply</param>
	public static void ChangeAudioSourceParameters(string soundSpawnToken, AudioSourceParameters audioSourceParameters)
	{
		if (Instance.SoundSpawns.ContainsKey(soundSpawnToken))
		{
			SoundSpawn soundSpawn = Instance.SoundSpawns[soundSpawnToken];
			ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);
		}
	}
}