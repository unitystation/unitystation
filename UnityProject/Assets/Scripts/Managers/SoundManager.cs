using System;
using AddressableReferences;
using Assets.Scripts.Messages.Server.SoundMessages;
using Mirror;
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

	[SerializeField] private GameObject soundSpawnPrefab = null;

	/// <summary>
	/// Library of AddressableAudioSource.  Might be loaded or not.
	/// </summary>
	/// <remarks>Always use GetAddressableAudioSourceFromCache if you want a loaded version</remarks>
	[HideInInspector] public readonly List<AddressableAudioSource> SoundsLibrary = new List<AddressableAudioSource>();

	/// <summary>
	/// Library of music paths (primaryKey)
	/// </summary>
	[HideInInspector] public readonly List<string> MusicLibrary = new List<string>();

	/// <summary>
	/// A list of all sounds currently playing
	/// </summary>
	/// <remarks>
	/// Thats useful for interrupting playing sounds, and preventing a sound to play over itself.
	/// Key is a Guid representing the token of the current playing sound.
	/// </remarks>
	public Dictionary<string, SoundSpawn> SoundSpawns = new Dictionary<string, SoundSpawn>();

	/// <summary>
	/// Dictionary of all sounds that have finished playing and are cashed for Quick playing
	/// </summary>
	public Dictionary<string, List<SoundSpawn>> NonplayingSounds = new Dictionary<string, List<SoundSpawn>>();

	/// <summary>
	/// Adds all musics to the music library.
	/// </summary>
	/// <remarks>
	/// Musics are identified in Addressable groups with a special label "Music"
	/// </remarks>
	private async Task AddMusicsToLibraryAsync()
	{
		// We build the library of musics location (by a special Label that identifies them).
		IList<IResourceLocation> resourceLocations =
			await Addressables.LoadResourceLocationsAsync("Music", typeof(GameObject)).Task;

		foreach (IResourceLocation resourceLocation in resourceLocations)
		{
			MusicLibrary.Add(resourceLocation.PrimaryKey);
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
	private static async Task<AddressableAudioSource> EnsureAddressableAudioSourceFromCache(
		List<AddressableAudioSource> addressableAudioSources)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		AddressableAudioSource addressableAudioSourceFromCache = null;
		lock (Instance.SoundsLibrary)
		{
			addressableAudioSourceFromCache =
				Instance.SoundsLibrary.FirstOrDefault(p => p.AssetAddress == addressableAudioSource.AssetAddress);
		}

		if (addressableAudioSourceFromCache == null)
		{
			lock (Instance.SoundsLibrary)
			{
				Instance.SoundsLibrary.Add(addressableAudioSource);
			}
		}

		// Ensure it's loaded and valid
		AudioSource audioSource;
		GameObject gameObject = await addressableAudioSource.Load();

		if (!gameObject.TryGetComponent(out audioSource))
		{
			Logger.LogError(
				$"AddressableAudioSource in SoundManager doesn't contain an AudioSource: {addressableAudioSource.AssetAddress}",
				Category.Addressables);
			return null;
		}

		return addressableAudioSource;
	}

	/// <summary>
	/// Get a fully loaded addressableAudioSource from the loaded cache.  This ensures that everything is ready to use.
	/// If more than one addressableAudioSource is provided, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <returns>A fully loaded and ready to use AddressableAudioSource</returns>
	public static async Task<AddressableAudioSource> GetAddressableAudioSourceFromCache(
		List<AddressableAudioSource> addressableAudioSources)
	{
		var addressableAudioSource = await EnsureAddressableAudioSourceFromCache(addressableAudioSources);
		return addressableAudioSource;
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
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	/// <returns>The SoundSpawn to be played</returns>
	private SoundSpawn GetNewSoundSpawn(AddressableAudioSource addressableAudioSource, AudioSource audioSource,
		string soundSpawnToken)
	{
		// The position doesn't matter at this point, but we need to provide one.
		GameObject soundSpawnObject = Instantiate(Instance.soundSpawnPrefab, Vector3.zero, Quaternion.identity);
		soundSpawnObject.transform.SetParent(this.gameObject.transform);
		soundSpawnObject.name = audioSource.name;
		SoundSpawn soundSpawn = soundSpawnObject.GetComponent<SoundSpawn>();
		soundSpawn.AudioSource.outputAudioMixerGroup = DefaultMixer;
		soundSpawn.SetAudioSource(audioSource);
		soundSpawn.assetAddress = addressableAudioSource.AssetAddress;
		if (soundSpawnToken != string.Empty)
		{
			soundSpawn.Token = soundSpawnToken;
			SoundSpawns[soundSpawn.Token] = soundSpawn;
		}

		return soundSpawn;
	}


	private SoundSpawn GetSoundSpawn(AddressableAudioSource addressableAudioSource, AudioSource audioSource,
		string soundSpawnToken)
	{
		if (NonplayingSounds.ContainsKey(addressableAudioSource.AssetAddress))
		{
			var ToReturn = NonplayingSounds[addressableAudioSource.AssetAddress][0];
			NonplayingSounds[addressableAudioSource.AssetAddress].RemoveAt(0);
			return ToReturn;
		}

		return GetNewSoundSpawn(addressableAudioSource, audioSource, soundSpawnToken);
	}

	public static void PlayNetworked(string addressableAudioSources, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30)
	{
		Logger.LogWarning("Sound needs to be converted to addressables " + addressableAudioSources);
	}


	/// <summary>
	/// Play sound for all clients.
	/// If more than one sound is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">List of sounds to be played.  If more than one sound is specified, one will be picked at random</param>
	public static async Task PlayNetworked(AddressableAudioSource addressableAudioSources, float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30)
	{
		if (addressableAudioSources == null || addressableAudioSources.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return;
		}

		var Toplay = new List<AddressableAudioSource>();
		Toplay.Add(addressableAudioSources);
		PlayNetworked(Toplay, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange);
	}

	/// <summary>
	/// Play sound for all clients.
	/// If more than one sound is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">List of sounds to be played.  If more than one sound is specified, one will be picked at random</param>
	public static async Task PlayNetworked(List<AddressableAudioSource> addressableAudioSources, float pitch = -1,
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

		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);
		PlaySoundMessage.SendToAll(addressableAudioSource, TransformState.HiddenPos, polyphonic, null, shakeParameters,
			audioSourceParameters);
	}


	public static string PlayNetworkedAtPos(string addressableAudioSource, Vector3 worldPos,
		AudioSourceParameters audioSourceParameters,
		bool polyphonic = false, bool Global = true, GameObject sourceObj = null,
		ShakeParameters shakeParameters = null)
	{
		Logger.LogWarning("Sound needs to be converted to addressables " + addressableAudioSource);
		return "";
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="Global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
	public static Task<string> PlayNetworkedAtPos(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		AudioSourceParameters audioSourceParameters,
		bool polyphonic = false, bool Global = true, GameObject sourceObj = null,
		ShakeParameters shakeParameters = null)
	{
		if (addressableAudioSource == null || addressableAudioSource.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return null;
		}

		return PlayNetworkedAtPos(new List<AddressableAudioSource> {addressableAudioSource}, worldPos,
			audioSourceParameters, polyphonic, Global, sourceObj, shakeParameters);
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
	/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
	public static async Task<string> PlayNetworkedAtPos(List<AddressableAudioSource> addressableAudioSources,
		Vector3 worldPos, AudioSourceParameters audioSourceParameters,
		bool polyphonic = false, bool Global = true, GameObject sourceObj = null,
		ShakeParameters shakeParameters = null)
	{
		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);

		if (Global)
		{
			return PlaySoundMessage.SendToAll(addressableAudioSource, worldPos, polyphonic, sourceObj, shakeParameters,
				audioSourceParameters);
		}
		else
		{
			return PlaySoundMessage.SendToNearbyPlayers(addressableAudioSource, worldPos, polyphonic, sourceObj,
				shakeParameters, audioSourceParameters);
		}
	}


	public static void PlayNetworkedAtPos(string addressableAudioSource, Vector3 worldPos, float pitch = -1,
		bool polyphonic = false, bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30,
		bool global = true, GameObject sourceObj = null)
	{
		Logger.LogWarning("Sound needs to be converted to addressables " + addressableAudioSource);
		return;
	}


	/// <summary>
	/// Play sound at given position for all clients.
	/// </summary>
	/// If more than one is specified, one will be picked at random.
	/// <param name="addressableAudioSource">The sound to be played.</param>
	public static void PlayNetworkedAtPos(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		float pitch = -1,
		bool polyphonic = false, bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30,
		bool global = true, GameObject sourceObj = null)
	{
		if (addressableAudioSource == null || addressableAudioSource.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return;
		}

		PlayNetworkedAtPos(new List<AddressableAudioSource>() {addressableAudioSource}, worldPos, pitch, polyphonic,
			shakeGround, shakeIntensity, shakeRange, global, sourceObj);
	}

	/// <summary>
	/// Play sound at given position for all clients.
	/// </summary>
	/// If more than one is specified, one will be picked at random.
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static void PlayNetworkedAtPos(List<AddressableAudioSource> addressableAudioSources, Vector3 worldPos,
		float pitch = -1,
		bool polyphonic = false, bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30,
		bool global = true, GameObject sourceObj = null)
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

		PlayNetworkedAtPos(addressableAudioSources, worldPos, audioSourceParameters, polyphonic, global, sourceObj,
			shakeParameters);
	}

	public static async Task PlayNetworkedForPlayer(GameObject recipient,
		AddressableAudioSource addressableAudioSources, float? pitch = null,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, GameObject sourceObj = null)
	{
		if (addressableAudioSources == null || addressableAudioSources.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return;
		}

		var Toplay = new List<AddressableAudioSource>();
		Toplay.Add(addressableAudioSources);
		PlayNetworkedForPlayer(recipient, Toplay, pitch, polyphonic, shakeGround, shakeIntensity, shakeRange,
			sourceObj);
	}

	/// <summary>
	/// Play sound for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="recipient">The player that will receive the sound</param>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="pitch">The pitch variation of the sound.  Null for default pitch.</param>
	public static async Task PlayNetworkedForPlayer(GameObject recipient,
		List<AddressableAudioSource> addressableAudioSources, float? pitch = null,
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

		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);
		PlaySoundMessage.Send(recipient, addressableAudioSource, TransformState.HiddenPos, polyphonic, sourceObj,
			shakeParameters, audioSourceParameters);
	}

	public static async Task PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos,
		string addressableAudioSources,
		float pitch = -1,
		bool polyphonic = false,
		bool shakeGround = false, byte shakeIntensity = 64, int shakeRange = 30, GameObject sourceObj = null)
	{
		Logger.LogWarning("Sound needs to be converted to addressables " + addressableAudioSources);
		return;
	}

	/// <summary>
	/// Serverside: Play sound at given position for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	public static async Task PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos,
		List<AddressableAudioSource> addressableAudioSources,
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

		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);
		PlaySoundMessage.Send(recipient, addressableAudioSource, worldPos, polyphonic, sourceObj, shakeParameters,
			audioSourceParameters);
	}


	/// <summary>
	/// Play a sound locally
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	public static async Task Play(AddressableAudioSource addressableAudioSource, string soundSpawnToken,
		AudioSourceParameters audioSourceParameters, bool polyphonic = false)
	{
		if (addressableAudioSource.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return;
		}

		Play(new List<AddressableAudioSource>() {addressableAudioSource}, soundSpawnToken, audioSourceParameters,
			polyphonic);
	}

	/// <summary>
	/// Play a sound locally
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	public static async Task Play(List<AddressableAudioSource> addressableAudioSources, string soundSpawnToken,
		AudioSourceParameters audioSourceParameters, bool polyphonic = false)
	{
		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);
		SoundSpawn soundSpawn =
			Instance.GetSoundSpawn(addressableAudioSource, addressableAudioSource.AudioSource, soundSpawnToken);
		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);

		Instance.PlaySource(soundSpawn, polyphonic,
			forceMixer: audioSourceParameters != null && audioSourceParameters.MixerType != MixerType.Unspecified);
	}


	public static async Task Play(AddressableAudioSource addressableAudioSources, string soundSpawnToken,
		float volume, float pitch = -1, float time = 0, bool oneShot = false,
		float pan = 0)
	{
		if (addressableAudioSources.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return;
		}

		Play(new List<AddressableAudioSource>() {addressableAudioSources}, soundSpawnToken, volume, pitch, time,
			oneShot, pan);
	}


	/// <summary>
	/// Play sound locally.
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	public static async Task Play(List<AddressableAudioSource> addressableAudioSources, string soundSpawnToken,
		float volume, float pitch = -1, float time = 0, bool oneShot = false,
		float pan = 0)
	{
		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);
		SoundSpawn soundSpawn =
			Instance.GetSoundSpawn(addressableAudioSource, addressableAudioSource.AudioSource, soundSpawnToken);

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
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	/// <param name="global">Should the sound be played for the default mixer or false to check if it should play muffled</param>
	/// <remarks>
	///		If Global is true, the sound may still be muffled if the source is configured with the muffled mixer.
	/// </remarks>
	public static async Task Play(AddressableAudioSource addressableAudioSource, string soundSpawnToken = "",
		bool polyphonic = false, bool global = true)
	{
		if (addressableAudioSource.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return;
		}

		Play(new List<AddressableAudioSource>() {addressableAudioSource}, soundSpawnToken, polyphonic, global);
	}

	/// <summary>
	/// Play sound locally.
	/// If more than one element is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	/// <param name="global">Should the sound be played for the default mixer or false to check if it should play muffled</param>
	/// <remarks>
	///		If Global is true, the sound may still be muffled if the source is configured with the muffled mixer.
	/// </remarks>
	public static async Task Play(List<AddressableAudioSource> addressableAudioSources, string soundSpawnToken,
		bool polyphonic = false, bool global = true)
	{
		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);
		var sound = Instance.GetSoundSpawn(addressableAudioSource, addressableAudioSource.AudioSource, soundSpawnToken);
		Instance.PlaySource(sound, polyphonic, global);
	}

	private void PlaySource(SoundSpawn source, bool polyphonic = false, bool Global = true, bool forceMixer = false)
	{
		if (!forceMixer)
		{
			if (!Global
			    && PlayerManager.LocalPlayer != null
			    && (MatrixManager.Linecast(PlayerManager.LocalPlayer.TileWorldPosition().To3Int(),
					    LayerTypeSelection.Walls, layerMask, source.RegisterTile.WorldPositionClient.To2Int().To3Int())
				    .ItHit))
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

	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	public static void PlayAtPosition(AddressableAudioSource addressableAudioSource, string soundSpawnToken,
		Vector3 worldPos, GameObject sourceObj,
		bool polyphonic = false,
		bool isGlobal = false,
		AudioSourceParameters audioSourceParameters = null)
	{
		if (addressableAudioSource.AssetAddress == string.Empty)
		{
			Logger.LogWarning(
				"Addressable audio sources not set/path is not present, look at log trace for responsible component");
			return;
		}

		PlayAtPosition(new List<AddressableAudioSource>() {addressableAudioSource},
			soundSpawnToken, worldPos, sourceObj, polyphonic, isGlobal, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// If more than one element is specified, one will be picked at random.
	/// This static method is for specifically attaching sound play to a target object (it will
	/// parent itself to the target and set its local position to Vector3.zero before playing)
	/// This is useful for moving objects that play sounds
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	public static void PlayAtPosition(List<AddressableAudioSource> addressableAudioSources, string soundSpawnToken,
		Vector3 worldPos, GameObject sourceObj,
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

		PlayAtPosition(addressableAudioSources, soundSpawnToken, worldPos, polyphonic, isGlobal, netId,
			audioSourceParameters);
	}


	/// <summary>
	/// Play sound locally at given world position.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	public static async Task PlayAtPosition(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		GameObject gameObject = null, string soundSpawnToken = "", bool polyphonic = false,
		bool isGlobal = false, AudioSourceParameters audioSourceParameters = null)
	{
		uint netId = NetId.Empty;
		if (gameObject != null)
		{
			netId = gameObject.NetId();
			if (netId == NetId.Invalid)
			{
				Logger.LogError("Provided Game object for PlayAtPosition  does not have a network identity " +
				                addressableAudioSource.AssetAddress);
				return;
			}
		}


		PlayAtPosition(new List<AddressableAudioSource>() {addressableAudioSource}, soundSpawnToken, worldPos,
			polyphonic, isGlobal, netId, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// If more than one element is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
	public static async Task PlayAtPosition(List<AddressableAudioSource> addressableAudioSources,
		string soundSpawnToken, Vector3 worldPos, bool polyphonic = false,
		bool isGlobal = false, uint netId = NetId.Empty, AudioSourceParameters audioSourceParameters = null)
	{
		AddressableAudioSource addressableAudioSource =
			await GetAddressableAudioSourceFromCache(addressableAudioSources);
		SoundSpawn soundSpawn =
			Instance.GetSoundSpawn(addressableAudioSource, addressableAudioSource.AudioSource, soundSpawnToken);

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

		Instance.PlaySource(soundSpawn, polyphonic, isGlobal,
			audioSourceParameters != null && audioSourceParameters.MixerType != MixerType.Unspecified);
	}

	private static void ApplyAudioSourceParameters(AudioSourceParameters audioSourceParameters, SoundSpawn soundSpawn)
	{
		AudioSource audioSource = soundSpawn.AudioSource;

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
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the sound to be stopped</returns>
	public static void StopNetworked(string soundSpawnToken)
	{
		StopSoundMessage.SendToAll(soundSpawnToken);
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

	public bool IsSoundPlaying(string soundSpawnToken)
	{
		if (Instance.SoundSpawns.ContainsKey(soundSpawnToken))
			return Instance.SoundSpawns[soundSpawnToken].IsPlaying;
		else
			return false;
	}
}