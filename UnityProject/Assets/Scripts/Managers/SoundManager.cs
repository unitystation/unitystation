﻿using System;
using AddressableReferences;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Messages.Server.SoundMessages;
using UnityEngine;
using UnityEngine.Audio;
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
		foreach (var sound in Instance.SoundSpawns)
		{
			if (sound.Value == null) //This probably doesn't happen anymore
			{
				Logger.LogWarning($"Could not remove SoundSpawn {sound} because its value was null!", Category.Addressables);
				continue;
			}
			sound.Value.AudioSource.Stop();
		}

		Instance.SoundSpawns.Clear();
	}

	/// <summary>
	/// Get a fully loaded addressableAudioSource from the loaded cache.  This ensures that everything is ready to use.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.</param>
	/// <returns>A fully loaded and ready to use AddressableAudioSource</returns>
	public static async Task<AddressableAudioSource> GetAddressableAudioSourceFromCache(AddressableAudioSource addressableAudioSource)
	{
    //Make sure it is a valid Addressable AudioSource
    if (addressableAudioSource == null || addressableAudioSource == default(AddressableAudioSource))
		{
			Logger.LogWarning("SoundManager recieved a null Addressable audio source, look at log trace for responsible component", Category.Addressables);
			return null;
		}
		if (string.IsNullOrEmpty(addressableAudioSource.AssetAddress))
		{
			Logger.LogWarning("SoundManager received a null address for an addressable, look at log trace for responsible component", Category.Addressables);
			return null;
		}
		if (addressableAudioSource.AssetAddress == "null")
		{
			Logger.LogWarning("SoundManager received an addressable with an address set to the string 'null', look at log trace for responsible component", Category.Addressables);
			return null;
		}
		if(await addressableAudioSource.HasValidAddress() == false) return null;

		//Try to get the Audio Source from cache, if its not there load it into cache
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
			addressableAudioSourceFromCache = addressableAudioSource;
		}

		//Ensure that the audio source is loaded
		GameObject gameObject = await addressableAudioSourceFromCache.Load();

		if (gameObject == null)
		{
			Logger.LogError(
				$"AddressableAudioSource in SoundManager failed to load from address: {addressableAudioSourceFromCache.AssetAddress}",
				Category.Addressables);
			return null;
		}

		if (gameObject.TryGetComponent(out AudioSource audioSource) == false)
		{
			Logger.LogError(
				$"AddressableAudioSource in SoundManager doesn't contain an AudioSource: {addressableAudioSourceFromCache.AssetAddress}",
				Category.Addressables);
			return null;
		}

		return addressableAudioSourceFromCache;
	}

	/// <summary>
	/// Get a fully loaded addressableAudioSource from the loaded cache.  This ensures that everything is ready to use.
	/// If more than one addressableAudioSource is provided, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">A list containing sounds to be played. If more than one is specified, one will be picked at random.</param>
	/// <returns>A fully loaded and ready to use AddressableAudioSource</returns>
	public static async Task<AddressableAudioSource> GetAddressableAudioSourceFromCache(List<AddressableAudioSource> addressableAudioSources)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		addressableAudioSource = await GetAddressableAudioSourceFromCache(addressableAudioSource);
		return addressableAudioSource;
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
			SoundSpawns.Add(soundSpawnToken, soundSpawn);
		}

		return soundSpawn;
	}


	/// <summary>
	/// Trys to get a Soundspawn from NonplayingSounds, otherwise gets a new one.
	/// This copies the AudioSource settings to the new SoundSpawn instance and returns it.
	/// </summary>
	/// <param name="audioSource">The AudioSource to copy</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	/// <returns>The SoundSpawn to be played</returns>
	private SoundSpawn GetSoundSpawn(AddressableAudioSource addressableAudioSource, AudioSource audioSource,
		string soundSpawnToken)
	{
		if (NonplayingSounds.ContainsKey(addressableAudioSource.AssetAddress) &&
			NonplayingSounds[addressableAudioSource.AssetAddress].Count > 0)
		{
			var ToReturn = NonplayingSounds[addressableAudioSource.AssetAddress][0];
			NonplayingSounds[addressableAudioSource.AssetAddress].RemoveAt(0);
			if (soundSpawnToken != "") //non addressables dont have a token
			{
				ToReturn.Token = soundSpawnToken;
				SoundSpawns.Add(soundSpawnToken, ToReturn);
			}
			return ToReturn;
		}

		return GetNewSoundSpawn(addressableAudioSource, audioSource, soundSpawnToken);
	}


	/// <summary>
	/// Plays a sound for all clients.
	/// </summary>
	/// <param name="addressableAudioSource">The sound to be played.</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="shakeParameters">Extra parameters that define the sound's associated shake</param>
	public static async Task PlayNetworked(AddressableAudioSource addressableAudioSource,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters())
	{
		addressableAudioSource = await GetAddressableAudioSourceFromCache(addressableAudioSource);
		PlaySoundMessage.SendToAll(addressableAudioSource, TransformState.HiddenPos, polyphonic, null, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Play sound from a list for all clients.
	/// If more than one sound is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">A list of sounds.  One will be played at random</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	public static async Task PlayNetworked(List<AddressableAudioSource> addressableAudioSources,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters())
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		await PlayNetworked(addressableAudioSource, audioSourceParameters, polyphonic, shakeParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// </summary>
	/// <param name="addressableAudioSource">The sound to be played.</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
	public static async Task<string> PlayNetworkedAtPosAsync(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false, bool global = true,
		ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		if (addressableAudioSource == null || string.IsNullOrEmpty(addressableAudioSource.AssetAddress) ||
			addressableAudioSource.AssetAddress == "null")
		{
			Logger.LogWarning($"SoundManager received a null AudioSource to be played at World Position: {worldPos}",
				Category.Addressables);
			return null;
		}

		addressableAudioSource = await GetAddressableAudioSourceFromCache(addressableAudioSource);;

		if (global)
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

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// If more than one sound is specified, the sound will be chosen at random
	/// </summary>
	/// <param name="addressableAudioSource">The sound to be played.</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
	public static async Task<string> PlayNetworkedAtPosAsync(List<AddressableAudioSource> addressableAudioSources,
		Vector3 worldPos, AudioSourceParameters audioSourceParameters = new AudioSourceParameters(),
		bool polyphonic = false, bool global = true, ShakeParameters shakeParameters = new ShakeParameters(),
		GameObject sourceObj = null)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		return await PlayNetworkedAtPosAsync(addressableAudioSource, worldPos, audioSourceParameters, polyphonic,
			global, shakeParameters, sourceObj);
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// Please use PlayNetworkedAtPosAsync if possible.
	/// </summary>
	/// <param name="addressableAudioSource">The sound to be played.</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
	public static void PlayNetworkedAtPos(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false, bool global = true,
		ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		PlayNetworkedAtPosAsync(addressableAudioSource, worldPos, audioSourceParameters, polyphonic,
			global, shakeParameters, sourceObj);
		return;
	}

	/// <summary>
	/// Serverside: Play sound at given position for all clients.
	/// If more than one sound is specified, the sound will be chosen at random
	/// Please use PlayNetworkedAtPosAsync if possible.
	/// </summary>
	/// <param name="addressableAudioSources">A list of sounds.  One will be played at random</param>
	/// <param name="worldPos">The position at which the sound is played</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
	public static void PlayNetworkedAtPos(List<AddressableAudioSource> addressableAudioSources,
		Vector3 worldPos, AudioSourceParameters audioSourceParameters = new AudioSourceParameters(),
		bool polyphonic = false, bool global = true, ShakeParameters shakeParameters = new ShakeParameters(),
		GameObject sourceObj = null)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		PlayNetworkedAtPosAsync(addressableAudioSource, worldPos, audioSourceParameters, polyphonic,
			global, shakeParameters, sourceObj);
		return;
	}


	/// <summary>
	/// Play sound for particular player.
	/// ("Doctor, there are voices in my head!")
	/// </summary>
	/// <param name="recipient">The player that will receive the sound</param>
	/// <param name="addressableAudioSource">The sound to be played.</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	public static async Task PlayNetworkedForPlayer(GameObject recipient, AddressableAudioSource addressableAudioSource,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		if (addressableAudioSource == null || string.IsNullOrEmpty(addressableAudioSource.AssetAddress) ||
			addressableAudioSource.AssetAddress == "null")
		{
			Logger.LogWarning($"SoundManager received a null AudioSource to be played for: {recipient.name}",
				Category.Addressables);
			return;
		}
		PlaySoundMessage.Send(recipient, addressableAudioSource, TransformState.HiddenPos, polyphonic,
			sourceObj, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Play sound for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="recipient">The player that will receive the sound</param>
	/// <param name="addressableAudioSources">A list of sounds.  One will be played at random</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	public static async Task PlayNetworkedForPlayer(GameObject recipient, List<AddressableAudioSource> addressableAudioSources,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		await PlayNetworkedForPlayer(recipient, addressableAudioSource, audioSourceParameters,
			polyphonic, shakeParameters, sourceObj);
	}

	/// <summary>
	/// Serverside: Play sound at given position for particular player.
	/// ("Doctor, there are voices in my head!")
	/// </summary>
	/// <param name="recipient">The player that will receive the sound</param>
	/// <param name="addressableAudioSource">The sound to be played.</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	public static async Task PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos,
		AddressableAudioSource addressableAudioSource, AudioSourceParameters audioSourceParameters = new AudioSourceParameters(),
		bool polyphonic = false, ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		if (addressableAudioSource == null || string.IsNullOrEmpty(addressableAudioSource.AssetAddress) ||
			addressableAudioSource.AssetAddress == "null")
		{
			Logger.LogWarning($"SoundManager received a null AudioSource to be played for: {recipient.name} at position: {worldPos}",
				Category.Addressables);
			return;
		}
		addressableAudioSource = await GetAddressableAudioSourceFromCache(addressableAudioSource);
		PlaySoundMessage.Send(recipient, addressableAudioSource, worldPos, polyphonic, sourceObj, shakeParameters,
			audioSourceParameters);
	}

	/// <summary>
	/// Serverside: Play sound at given position for particular player.
	/// ("Doctor, there are voices in my head!")
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="recipient">The player that will receive the sound</param>
	/// <param name="addressableAudioSources">A list of sounds.  One will be played at random</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source.</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	/// <param name="sourceObj">The object that is the source of the sound</param>
	public static async Task PlayNetworkedForPlayerAtPos(GameObject recipient, Vector3 worldPos,
		List<AddressableAudioSource> addressableAudioSources, AudioSourceParameters audioSourceParameters = new AudioSourceParameters(),
		bool polyphonic = false, ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		await PlayNetworkedForPlayerAtPos(recipient, worldPos, addressableAudioSource, audioSourceParameters,
			polyphonic, shakeParameters, sourceObj);
	}

	/// <summary>
	/// Play a sound locally
	/// </summary>
	/// <param name="addressableAudioSource">The sound to be played.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	public static async Task Play(AddressableAudioSource addressableAudioSource, string soundSpawnToken = "",
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false)
	{
		if(GameData.IsHeadlessServer)
			return;

		addressableAudioSource = await GetAddressableAudioSourceFromCache(addressableAudioSource);
		SoundSpawn soundSpawn =
			Instance.GetSoundSpawn(addressableAudioSource, addressableAudioSource.AudioSource, soundSpawnToken);
		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);
		Instance.PlaySource(soundSpawn, polyphonic, true, audioSourceParameters.MixerType);
	}

	/// <summary>
	/// Play a sound locally
	/// If more than one is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">The sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	/// <param name="audioSourceParameters">Parameters for how to play the sound</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	public static async Task Play(List<AddressableAudioSource> addressableAudioSources, string soundSpawnToken = "",
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		await Play(addressableAudioSource, soundSpawnToken, audioSourceParameters, polyphonic);
	}

	/// <summary>
	/// Plays a SoundSpawn.
	/// </summary>
	/// <param name="source">The SoundSpawn to be played</param>
	/// <param name="polyphonic">Should the sound be played polyphonically</param>
	/// <param name="global">Does everyone will receive the sound our just nearby players</param>
	/// <param name="mixerType">The type of mixer to use</param>
	private void PlaySource(SoundSpawn source, bool polyphonic = false, bool global = true, MixerType mixerType = MixerType.Master)
	{
		if (global == false
		    && PlayerManager.LocalPlayer != null
		    && MatrixManager.Linecast(PlayerManager.LocalPlayer.TileWorldPosition().To3Int(),
			    LayerTypeSelection.Walls, layerMask, source.transform.position.To2Int().To3Int())
			    .ItHit)
			{
				source.AudioSource.outputAudioMixerGroup = soundManager.MuffledMixer;
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
	/// This static method is for specifically attaching sound play to a target object (it will
	/// parent itself to the target and set its local position to Vector3.zero before playing)
	/// This is useful for moving objects that play sounds
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	public static void PlayAtPositionAttached(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		GameObject gameObject, string soundSpawnToken,	bool polyphonic = false, bool isGlobal = false,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
	{
		PlayAtPositionAttached(new List<AddressableAudioSource> {addressableAudioSource}, worldPos, gameObject,
			soundSpawnToken, polyphonic, isGlobal, audioSourceParameters);
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
	public static void PlayAtPositionAttached(List<AddressableAudioSource> addressableAudioSources, Vector3 worldPos,
		GameObject gameObject, string soundSpawnToken,	bool polyphonic = false, bool isGlobal = false,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
	{
		var netId = NetId.Empty;
		if (gameObject != null)
		{
			var netB = gameObject.GetComponent<NetworkBehaviour>();
			if (netB != null)
			{
				netId = netB.netId;
			}
		}

		PlayAtPosition(addressableAudioSources, worldPos, soundSpawnToken, polyphonic, isGlobal, netId,
			audioSourceParameters);
	}


	/// <summary>
	/// Play sound locally at given world position.
	/// </summary>
	/// <param name="addressableAudioSource">Sound to be played.</param>
	/// <param name="soundSpawnToken">The SoundSpawn Token that identifies the same sound spawn instance across server and clients</returns>
	public static async Task PlayAtPosition(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		GameObject gameObject = null, string soundSpawnToken = "", bool polyphonic = false,
		bool isGlobal = false, AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
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


		_ = PlayAtPosition(new List<AddressableAudioSource>() {addressableAudioSource}, worldPos, soundSpawnToken,
			polyphonic, isGlobal, netId, audioSourceParameters);
	}

	/// <summary>
	/// Play sound locally at given world position.
	/// If more than one element is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">Sound to be played.  If more than one is specified, one will be picked at random.</param>
	/// <param name="soundSpawnToken">The token that identifies the SoundSpawn uniquely among the server and all clients </param>
	public static async Task PlayAtPosition(List<AddressableAudioSource> addressableAudioSources,
		Vector3 worldPos, string soundSpawnToken, bool polyphonic = false,
		bool isGlobal = false, uint netId = NetId.Empty, AudioSourceParameters audioSourceParameters = new AudioSourceParameters())
	{
		if(GameData.IsHeadlessServer)
			return;

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

		Instance.PlaySource(soundSpawn, polyphonic, isGlobal, audioSourceParameters.MixerType);
	}

	/// <Summary>
	/// Used to apply incomplete AudioSourceParameters to a Sound, such as changing pitch or volume.
	/// As a Struct, AudioSourceParameters initializes zeroed out, so to prevent sounds from getting
	/// messed up some limitations apply.  For complete control use ForceAudioSourceParameters.
	/// </Summary>
	private static void ApplyAudioSourceParameters(AudioSourceParameters audioSourceParameters, SoundSpawn soundSpawn)
	{
		AudioSource audioSource = soundSpawn.AudioSource;

		//Volume can be 0 for two reasons: it is uninitialized or it is supposed to be 0.
		//If it is supposed to be 0, IsMute should be set to true.  If its not 0, that's the value
		//we want, otherwise no changes.
		if(audioSourceParameters.IsMute == true)
		{
			audioSource.volume = 0;
		}
		else if(audioSourceParameters.Volume > 0)
		{
			audioSource.volume = audioSourceParameters.Volume;
		}

    //Pitch should never be 0.  A negative pitch plays the sound backwards.
		if(audioSourceParameters.Pitch != 0)
			audioSource.pitch = audioSourceParameters.Pitch;
		else if(audioSource.pitch == 0)
			audioSource.pitch = 1;

		//The following parameters have some limitations that shouldn't really come up
		//Note if the sound's default value for a parameter is 0, the limitation does not apply.

		//Cannot seek to timestamp 0 for a sound that does not start at the beginning by default
		if(audioSourceParameters.Time != 0)
			audioSource.time = audioSourceParameters.Time;

		//-1 is left, 0 is center, 1 is right.
		//Cannot pan to center for sounds that are panned by default
		if(audioSourceParameters.Pan != 0)
			audioSource.panStereo = audioSourceParameters.Pan;

		//0 is 2D and ignores max/min distance, 1 is 3d and obeys them
		//Cannot convert sounds that are 3D by default to 2D
		if(audioSourceParameters.SpatialBlend != 0)
			audioSource.spatialBlend = audioSourceParameters.SpatialBlend;

		//Cannot change the minimum distance for audio falloff to 0
		if(audioSourceParameters.MinDistance != 0)
			audioSource.minDistance = audioSourceParameters.MinDistance;

		//Cannot change the max distance for falloff to 0 (why would you want that?)
		if(audioSourceParameters.MaxDistance != 0)
			audioSource.maxDistance = audioSourceParameters.MaxDistance;

		//Cannot convert non-mono sounds to mono
		if(audioSourceParameters.Spread != 0)
			audioSource.spread = audioSourceParameters.Spread;

		audioSource.outputAudioMixerGroup = audioSourceParameters.MixerType == MixerType.Master
				? Instance.DefaultMixer : Instance.MuffledMixer;

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

	/// <Summary>
	/// Completely overwrites AudioSourceParameters of a Sound to any value.
	/// Only use this if you have a known entry for all parameters, otherwise the sound will
	/// not play properly (eg, having no entry for pitch will make the sound never start or finish)
	/// </Summary>
	private static void ForceAudioSourceParameters(AudioSourceParameters audioSourceParameters, SoundSpawn soundSpawn){
		AudioSource audioSource = soundSpawn.AudioSource;

		audioSource.volume = audioSourceParameters.Volume;
		audioSource.pitch = audioSourceParameters.Pitch;
		audioSource.time = audioSourceParameters.Time;
		audioSource.panStereo = audioSourceParameters.Pan;
		audioSource.spatialBlend = audioSourceParameters.SpatialBlend;
		audioSource.minDistance = audioSourceParameters.MinDistance;
		audioSource.maxDistance = audioSourceParameters.MaxDistance;
		audioSource.spread = audioSourceParameters.Spread;
		audioSource.outputAudioMixerGroup = audioSourceParameters.MixerType == MixerType.Master
			? Instance.DefaultMixer : Instance.MuffledMixer;
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
