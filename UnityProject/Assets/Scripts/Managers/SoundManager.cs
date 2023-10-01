using AddressableReferences;
using Mirror;
using System.Collections.Generic;
using System.Threading.Tasks;
using Messages.Server.SoundMessages;
using UnityEngine;
using UnityEngine.Audio;
using Audio.Containers;
using Core.Sound;
using Logs;
using Shared.Util;

/// <summary>
/// Manager that allows to play sounds.
/// Should they be local (single client) or networked across one or more client.
/// </summary>
public class SoundManager : MonoBehaviour
{
	private static LayerMask layerMask;

	private static SoundManager soundManager;

	[SerializeField] private GameObject soundSpawnPrefab = null;

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

	public static SoundManager Instance => FindUtils.LazyFindObject(ref soundManager);

	#region Lifecycle

	private void Awake()
	{
		Init();
	}

	private void Init()
	{
		layerMask = LayerMask.GetMask("Walls", "Door Closed");
	}

	public void Clear()
	{
		foreach (var sound in Instance.SoundSpawns)
		{
			if (sound.Value == null) //This probably doesn't happen anymore
			{
				Loggy.LogWarning($"Could not remove SoundSpawn {sound} because its value was null!", Category.Audio);
				continue;
			}
			sound.Value.AudioSource.Stop();
		}

		Instance.SoundSpawns.Clear();
	}

	#endregion

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
		soundSpawn.AudioSource.outputAudioMixerGroup = AudioManager.Instance.SFXMixer;
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
	public static string PlayNetworked(AddressableAudioSource addressableAudioSource,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters())
	{
		return PlaySoundMessage.SendToAll(addressableAudioSource, TransformState.HiddenPos, polyphonic, null, shakeParameters, audioSourceParameters);
	}

	/// <summary>
	/// Play sound from a list for all clients.
	/// If more than one sound is specified, one will be picked at random.
	/// </summary>
	/// <param name="addressableAudioSources">A list of sounds.  One will be played at random</param>
	/// <param name="audioSourceParameters">Extra parameters of the audio source</param>
	/// <param name="polyphonic">Is the sound to be played polyphonic</param>
	/// <param name="shakeParameters">Camera shake effect associated with this sound</param>
	public static string PlayNetworked(List<AddressableAudioSource> addressableAudioSources,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters())
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();
		return PlayNetworked(addressableAudioSource, audioSourceParameters, polyphonic, shakeParameters);
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
	/// <param name="attachToSource">Sound follows the source object</param>
	/// <returns>The SoundSpawn Token generated that identifies the same sound spawn instance across server and clients</returns>
	public static async Task<string> PlayNetworkedAtPosAsync(AddressableAudioSource addressableAudioSource, Vector3 worldPos,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false, bool global = true,
		ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null, bool attachToSource = false)
	{
		if (addressableAudioSource == null || string.IsNullOrEmpty(addressableAudioSource.AssetAddress) ||
			addressableAudioSource.AssetAddress == "null")
		{
			Loggy.LogWarning($"SoundManager received a null AudioSource to be played at World Position: {worldPos}",
				Category.Audio);
			return null;
		}

		addressableAudioSource = await AudioManager.GetAddressableAudioSourceFromCache(addressableAudioSource);

		if (global)
		{
			return PlaySoundMessage.SendToAll(addressableAudioSource, worldPos, polyphonic, sourceObj, shakeParameters,
				audioSourceParameters, attachToSource);
		}
		else
		{
			return PlaySoundMessage.SendToNearbyPlayers(addressableAudioSource, worldPos, polyphonic, sourceObj,
				shakeParameters, audioSourceParameters, attachToSource);
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
		_ = PlayNetworkedAtPosAsync(addressableAudioSource, worldPos, audioSourceParameters, polyphonic,
			global, shakeParameters, sourceObj);
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
		_ = PlayNetworkedAtPosAsync(addressableAudioSource, worldPos, audioSourceParameters, polyphonic,
			global, shakeParameters, sourceObj);
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
	public static void PlayNetworkedForPlayer(GameObject recipient, AddressableAudioSource addressableAudioSource,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		if (addressableAudioSource == null || string.IsNullOrEmpty(addressableAudioSource.AssetAddress) ||
			addressableAudioSource.AssetAddress == "null")
		{
			Loggy.LogWarning($"SoundManager received a null AudioSource to be played for: {recipient.name}",
				Category.Audio);
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
	public static void PlayNetworkedForPlayer(GameObject recipient, List<AddressableAudioSource> addressableAudioSources,
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool polyphonic = false,
		ShakeParameters shakeParameters = new ShakeParameters(), GameObject sourceObj = null)
	{
		AddressableAudioSource addressableAudioSource = addressableAudioSources.PickRandom();

		PlayNetworkedForPlayer(recipient, addressableAudioSource, audioSourceParameters,
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
			Loggy.LogWarning($"SoundManager received a null AudioSource to be played for: {recipient.name} at position: {worldPos}",
				Category.Audio);
			return;
		}
		addressableAudioSource = await AudioManager.GetAddressableAudioSourceFromCache(addressableAudioSource);
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

		addressableAudioSource = await AudioManager.GetAddressableAudioSourceFromCache(addressableAudioSource);
		if(addressableAudioSource == null)
		{
			Loggy.LogError("Cannot play sound! Sound is null!");
			return;
		}
		SoundSpawn soundSpawn =
			Instance.GetSoundSpawn(addressableAudioSource, addressableAudioSource.AudioSource, soundSpawnToken);
		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);
		Instance.PlaySource(soundSpawn, polyphonic, true);
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
	private void PlaySource(SoundSpawn source, bool polyphonic = false, bool global = true)
	{
		if (global == false && PlayerManager.LocalPlayerObject != null)
		{
			SoundPhysics.EvaluateAndRouteSoundToMixer(source);
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
		AudioSourceParameters audioSourceParameters = new AudioSourceParameters(), bool networked = false)
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

		_ = PlayAtPosition(addressableAudioSources, worldPos, soundSpawnToken, polyphonic, isGlobal, netId, audioSourceParameters);
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
				Loggy.LogError("Provided Game object for PlayAtPosition  does not have a network identity " +
				                addressableAudioSource.AssetAddress, Category.Audio);
				return;
			}
		}

		await PlayAtPosition(new List<AddressableAudioSource>() {addressableAudioSource}, worldPos, soundSpawnToken,
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
		if (GameData.IsHeadlessServer) return;

		AddressableAudioSource addressableAudioSource =
			await AudioManager.GetAddressableAudioSourceFromCache(addressableAudioSources);
		SoundSpawn soundSpawn =
			Instance.GetSoundSpawn(addressableAudioSource, addressableAudioSource.AudioSource, soundSpawnToken);
		var soundTransform = soundSpawn.transform;

		ApplyAudioSourceParameters(audioSourceParameters, soundSpawn);

		if (netId != NetId.Empty)
		{
			var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
			if (spawned.TryGetValue(netId, out var objectToPlayAt))
			{
				soundTransform.parent = objectToPlayAt.transform;
				soundTransform.localPosition = Vector3.zero;

				Instance.PlaySource(soundSpawn, polyphonic, isGlobal);
				return;
			}
		}

		var point = MatrixManager.AtPoint(worldPos, CustomNetworkManager.IsServer);
		soundTransform.parent = point != null ? point.Objects.transform : Instance.transform;
		soundTransform.position = worldPos;

		Instance.PlaySource(soundSpawn, polyphonic, isGlobal);
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

		audioSource.outputAudioMixerGroup = Instance.CalcAudioMixerGroup(audioSourceParameters.MixerType);

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
		audioSource.outputAudioMixerGroup = Instance.CalcAudioMixerGroup(audioSourceParameters.MixerType);
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
	/// Determine which AudioMixerGroup to use based on MixerType enum
	/// </summary>
	/// <param name="mixerType">MixterType enum/returns>
	private AudioMixerGroup CalcAudioMixerGroup (MixerType mixerType)
	{
		switch(mixerType)
			{
				case MixerType.Music:
					return AudioManager.Instance.MusicMixer;
				case MixerType.Muffled:
					return AudioManager.Instance.SFXMuffledMixer;
				case MixerType.Ambient:
					return AudioManager.Instance.AmbientMixer;
				case MixerType.JukeBox:
					return AudioManager.Instance.JukeboxMixer;
				default:
					return AudioManager.Instance.SFXMixer;
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
			Instance.SoundSpawns[soundSpawnToken]?.AudioSource.Stop();
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

			if (soundSpawn == null)
			{
				Debug.LogError($"Unable to change audio parameters, soundSpawn was null");
				return;
			}

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
