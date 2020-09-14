using Assets.Scripts.Messages.Server.SoundMessages;
using Mirror;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
	public AudioMixerGroup DefaultMixer;

	public AudioMixerGroup MuffledMixer;

	private static LayerMask layerMask;

	private static SoundManager soundManager;

	/// <summary>
	/// Library of sound AssetReferences and their AudioSource if loaded
	/// </summary>
	/// <remarks>
	/// If AudioSource is null, it means it's not loaded.
	/// </remarks>
	[HideInInspector]
	public readonly Dictionary<AssetReference, AudioSource> SoundsLibrary = new Dictionary<AssetReference, AudioSource>();

	/// <summary>
	/// Library of music paths (primaryKey) and their AudioSource if loaded
	/// </summary>
	/// <remarks>
	/// If AudioSource is null, it means it's not loaded.
	/// </remarks>
	[HideInInspector]
	public readonly Dictionary<string, AudioSource> MusicLibrary = new Dictionary<string, AudioSource>();

	/// <summary>
	/// A list of all sounds currently playing
	/// </summary>
	/// <remarks>
	/// Thats useful for interrupting playing sounds, and preventing a sound to play over itself.
	/// Key is a Guid representing the token of the current playing sound.
	/// </remarks>
	private Dictionary<string, SoundSpawn> SoundSpawns = new Dictionary<string, SoundSpawn>();

	private readonly Dictionary<string, string[]> soundPatterns = new Dictionary<string, string[]>();

	/// <summary>
	/// Load the AudioSource of a music inside the library and returns it.
	/// </summary>
	/// <param name="primaryKey">The primary key of the music to load</param>
	/// <returns>The AudioSource component of the music</returns>
	public async Task<AudioSource> GetMusicAsync(string primaryKey)
	{
		if (MusicLibrary[primaryKey] == null)
		{
			GameObject music = await Addressables.LoadAssetAsync<GameObject>(primaryKey).Task;
			MusicLibrary[primaryKey] = music.GetComponent<AudioSource>();
		}

		return MusicLibrary[primaryKey];
	}

	/// <summary>
	/// Unload the music by it's primaryKey, freeing resource RAM usage
	/// </summary>
	/// <param name="primaryKey">The primary of the music to unload </param>
	public void UnloadMusic(string primaryKey)
	{
		if (MusicLibrary[primaryKey] != null)
		{
			Addressables.ReleaseInstance(MusicLibrary[primaryKey].gameObject);
			MusicLibrary[primaryKey] = null;
		}
	}

	/// <summary>
	/// Load the AudioSource of a sound inside the library and returns it.
	/// </summary>
	/// <param name="assetReference">The assetReference to load</param>
	/// <returns>The AudioSource component of the sound</returns>
	private async Task<AudioSource> GetSoundAsync(AssetReference assetReference)
	{
		if (SoundsLibrary[assetReference] == null)
		{
			GameObject sound = await assetReference.LoadAssetAsync<GameObject>().Task;
			SoundsLibrary[assetReference] = sound.GetComponent<AudioSource>();
		}

		return SoundsLibrary[assetReference];
	}


	/// <summary>
	/// Get all sounds associated to the SoundManager and put them in the library.
	/// </summary>
	/// <remarks>That doesn't load the objets themselves, that only assemble the library</remarks>
	private void AddSoundsToLibraryRecursive(Transform rootTransform)
	{
		int childCount = rootTransform.childCount;

		AssetReferenceLibrary assetReferenceLibrary = null;
		if (TryGetComponent<AssetReferenceLibrary>(out assetReferenceLibrary))
		{
			foreach (AssetReference assetReference in assetReferenceLibrary.AssetReferences)
				SoundsLibrary.Add(assetReference, null);
		}

		for (int childIndex = 0; childIndex < childCount; childIndex++)
		{
			AddSoundsToLibraryRecursive(rootTransform.GetChild(childIndex));
		}
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
			MusicLibrary.Add(resourceLocation.PrimaryKey, null);
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

		// Load all other sounds to the sound library
		AddSoundsToLibraryRecursive(transform);
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


	private static async Task<AudioSource> GetAudioSourceFromLibrary(AssetReference assetReference)
	{
		if (!Instance.SoundsLibrary.ContainsKey(assetReference))
			Logger.LogError($"Unknown AssetReference in SoundManager: {assetReference.SubObjectName}");

		AudioSource audioSource = Instance.SoundsLibrary[assetReference];

		if (audioSource == null)
		{
			GameObject gameObject = await assetReference.LoadAssetAsync<GameObject>().Task;
			audioSource = gameObject.GetComponent<AudioSource>();
		}

		return audioSource;
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
		SoundSpawn soundSpawn = new SoundSpawn(audioSource);
		SoundSpawns.Add(soundSpawn.Token, soundSpawn);
		return soundSpawn;
	}

	/// <summary>
	/// Play sound for all clients.
	/// If more than one sound is specified, one will be picked at random.
	/// </summary>
	[Server]
	public static void PlayNetworked(List<AssetReference> assetReferences, float pitch = -1,
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

		PlaySoundMessage.SendToAll(assetReferences.PickRandom().AssetGUID, TransformState.HiddenPos, polyphonic, null, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// If more than one sound is specified, the sound will be chosen at random
	/// </summary>
	/// <param name="assetReferences">The sound to be played.  If more than one is specified, a single one will be picked at random</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="Global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	[Server]
	public static void PlayNetworkedAtPos(List<AssetReference> assetReferences, Vector3 worldPos, AudioSourceParameters audioSourceParameters,
		bool polyphonic = false, bool Global = true, GameObject sourceObj = null, ShakeParameters shakeParameters = null)
	{
		string soundGuid = assetReferences.PickRandom().AssetGUID;

		if (Global)
		{
			PlaySoundMessage.SendToAll(soundGuid, worldPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
		}
		else
		{
			PlaySoundMessage.SendToNearbyPlayers(soundGuid, worldPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
		}
	}


	/// <summary>
	/// Play sound at given position for all clients.
	/// </summary>
	/// If more than one is specified, one will be picked at random.
	/// <param name="assetReferences">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	[Server]
	public static void PlayNetworkedAtPos(List<AssetReference> assetReferences, Vector3 worldPos, float pitch = -1,
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

		PlayNetworkedAtPos(assetReferences, worldPos, audioSourceParameters, polyphonic, global, sourceObj, shakeParameters);
	}

	/// <summary>
	/// Play sound for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="recipient">The player that will receive the sound</param>
	/// <param name="assetReferences">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="pitch">The pitch variation of the sound.  Null for default pitch.</param>
	[Server]
	public static void PlayNetworkedForPlayer(GameObject recipient, List<AssetReference> assetReferences, float? pitch = null,
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

		string soundGuid = assetReferences.PickRandom().AssetGUID;
		PlaySoundMessage.Send(recipient, soundGuid, TransformState.HiddenPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="assetReferences">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static void PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos, List<AssetReference> assetReferences,
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

		string soundGuid = assetReferences.PickRandom().AssetGUID;
		PlaySoundMessage.Send(recipient, soundGuid, worldPos, polyphonic, sourceObj, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Play a sound locally
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="assetReferences">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	[Client]
	public static async void Play(List<AssetReference> assetReferences, AudioSourceParameters audioSourceParameters, bool polyphonic = false)
	{
		AssetReference assetReference = assetReferences.PickRandom();

		AudioSource audioSource = await GetAudioSourceFromLibrary(assetReference);
		SoundSpawn soundSpawn = Instance.GetNewSoundSpawn(audioSource);
		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);

		Instance.PlaySource(soundSpawn, polyphonic, true, audioSourceParameters != null && audioSourceParameters.MixerType != MixerType.Unspecified);
	}

	/// <summary>
	/// Play sound locally.
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="assetReferences">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	[Client]
	public static async void Play(List<AssetReference> assetReferences, float volume, float pitch = -1, float time = 0, bool oneShot = false,
		float pan = 0)
	{
		AssetReference assetReference = assetReferences.PickRandom();
		AudioSource audioSource = await GetAudioSourceFromLibrary(assetReference);

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
	/// <param name="assetReferences">AssetReference of the sound to be played.  (Or chosen at random if many)</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	/// <param name="global">Should the sound be played for the default mixer or false to check if it should play muffled</param>
	/// <remarks>
	///		If Global is true, the sound may still be muffled if the source is configured with the muffled mixer.
	/// </remarks>
	[Client]
	public static async void Play(List<AssetReference> assetReferences, bool polyphonic = false, bool global = true)
	{
		AssetReference assetReference = assetReferences.PickRandom();

		AudioSource audioSource = await GetAudioSourceFromLibrary(assetReference);
		Instance.PlaySource(Instance.GetNewSoundSpawn(audioSource), polyphonic, global);
	}

	private void PlaySource(SoundSpawn source, bool polyphonic = false, bool Global = true, bool forceMixer = false)
	{
		if (!forceMixer)
		{
			if (!Global
				&& PlayerManager.LocalPlayer != null
				&& Physics2D.Linecast(PlayerManager.LocalPlayer.TileWorldPosition(), source.Transform.position, layerMask))
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


	/// <summary>
	/// Play sound locally at given world position.
	/// If more than one element is specified, one will be picked at random.
	/// This static method is for specifically attaching sound play to a target object (it will
	/// parent itself to the target and set its local position to Vector3.zero before playing)
	/// This is useful for moving objects that play sounds
	/// </summary>
	/// <param name="assetReferences">AssetReference of the sound to be played.  (Or chosen at random if many)</param>
	[Client]
	public static void PlayAtPosition(List<AssetReference> assetReferences, Vector3 worldPos, GameObject sourceObj,
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

		PlayAtPosition(assetReferences, worldPos, polyphonic, isGlobal, netId, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// If more than one element is specified, one will be picked at random.
	/// </summary>
	/// <param name="assetReferences">AssetReference of the sound to be played.  (Or chosen at random if many)</param>
	[Client]
	public static async void PlayAtPosition(List<AssetReference> assetReferences, Vector3 worldPos, bool polyphonic = false,
		bool isGlobal = false, uint netId = NetId.Empty, AudioSourceParameters audioSourceParameters = null)
	{
		AssetReference assetReference = assetReferences.PickRandom();
		AudioSource audioSource = await GetAudioSourceFromLibrary(assetReference);

		SoundSpawn soundSpawn = Instance.GetNewSoundSpawn(audioSource);

		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);

		if (netId != NetId.Empty)
		{
			if (NetworkIdentity.spawned.ContainsKey(netId))
			{
				soundSpawn.Transform.parent = NetworkIdentity.spawned[netId].transform;
				soundSpawn.Transform.localPosition = Vector3.zero;
			}
			else
			{
				soundSpawn.Transform.parent = Instance.transform;
				soundSpawn.Transform.position = worldPos;
			}
		}
		else
		{
			soundSpawn.Transform.parent = Instance.transform;
			soundSpawn.Transform.position = worldPos;
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