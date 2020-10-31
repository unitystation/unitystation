using AddressableReferences;
using Assets.Scripts.Messages.Server.SoundMessages;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Systems.Electricity;
using UnityEngine;

/// <summary>
/// A machine that plays music choosen by it's user's tastes in a cool place like a lounge or a bar.
/// </summary>
public class Jukebox : NetworkBehaviour, IAPCPowered
{
	/// <summary>
	/// How many watts at 240 V the Jukebox uses when not in use
	/// </summary>
	[SerializeField]
	private int StandByWattUsage = 5;

	/// <summary>
	/// How many watts at 240 V the Jukebox uses when it is in use
	/// </summary>
	[SerializeField]
	private int InUseWattUsage = 15;

	[SerializeField]
	private SpriteHandler spriteHandler = null;

	// Sprites for when the jukebox is idle, playing, damaged.
	[SerializeField]
	private SpriteDataSO SpriteIdle = null;

	[SerializeField]
	private SpriteDataSO SpritePlaying = null;

	[SerializeField]
	private SpriteDataSO SpriteDamaged = null;

	[SerializeField]
	private float MinSoundDistance = 4;

	[SerializeField]
	private float MaxSoundDistance = 10;

	[SerializeField]
	[Range(0, 1)]
	private float Volume = 1;

	[SerializeField]
	[Range(0, 360)]
	private float Spread = 0;


	private SpriteRenderer spriteRenderer;
	/// <summary>
	/// The current state of the jukebox powered/overpowered/underpowered/no power
	/// </summary>
	private PowerStates CurrentState;

	/// <summary>
	/// The current state of the jukebox powered/overpowered/underpowered/no power
	/// </summary>
	private APCPoweredDevice APCConnectionHandler;

	private Integrity integrity;
	private APCPoweredDevice power;
	private RegisterTile registerTile;
	private int currentSongTrackIndex = 0;
	private float startPlayTime;

	private string soundSpawnToken;

	private async Task<AddressableAudioSource> GetAddressableAudioSourceFromCache()
	{
		AddressableAudioSource addressableAudioSourceParam = new AddressableAudioSource(SoundManager.Instance.MusicLibrary.ElementAt(currentSongTrackIndex));
		AddressableAudioSource addressableAudioSourceFromCache = await SoundManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource> { addressableAudioSourceParam });
		return addressableAudioSourceFromCache;
	}

	public bool IsPlaying { get; set; } = false;

	public string TrackPosition
	{
		get
		{
			return $"{currentSongTrackIndex + 1} / {SoundManager.Instance.MusicLibrary.Count}";
		}
	}

	public async Task<string> GetSongNameAsync()
	{
		AddressableAudioSource addressableAudioSourceFromCache = await GetAddressableAudioSourceFromCache();
		AudioSource audioSource = addressableAudioSourceFromCache.AudioSource;
		return $"{audioSource.clip.name.Split('_')[0]}";
	}

	public async Task<string> GetArtistNameAsync()
	{
		AddressableAudioSource addressableAudioSourceFromCache = await GetAddressableAudioSourceFromCache();
		AudioSource audioSource = addressableAudioSourceFromCache.AudioSource;
		string songName = audioSource.clip.name;
		string artist = songName.Contains("_") ? songName.Split('_')[1] : "Unknown";
		return $"{artist}";
	}

	public string PlayStopButtonPrefabImage
	{
		get
		{
			return IsPlaying ? "GUI_Jukebox_Stop" : "GUI_Jukebox_Play";
		}
	}

	public void PowerNetworkUpdate(float Voltage)
	{
		// Nothing really.  Only the state matters.  (See StateUpdate).
	}

	public void StateUpdate(PowerStates State)
	{
		CurrentState = State;
		if (spriteHandler == null)
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		if (APCConnectionHandler == null)
		{
			APCConnectionHandler = GetComponentInChildren<APCPoweredDevice>();
		}

		// For a future iteration, allow the jukebox to be connected to the power grid.
		/*
		if (State <= PowerStates.On)
		{
			Stop();
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Jukebox, TabAction.Close);
		}
		*/

		// StateUpdate might happen before start
		if (IsPlaying)
		{
			APCConnectionHandler.Wattusage = InUseWattUsage;
		}
		else
		{
			APCConnectionHandler.Wattusage = StandByWattUsage;
		}
	}

	// Start is called before the first frame update
	private void Start()
	{
		// We want the same musics that are in the lobby,
		// so, I copy it's playlist here instead of managing two different playlists in UnityEditor.
		APCConnectionHandler = GetComponent<APCPoweredDevice>();
		power = GetComponent<APCPoweredDevice>();
		registerTile = GetComponent<RegisterTile>();
		UpdateGUIAsync();

		UpdateManager.Add(CheckSongFinished, 1.0f);
	}

	private void Awake()
	{
		integrity = GetComponent<Integrity>();
		integrity.OnApplyDamage.AddListener(OnDamageReceived);
	}

	private void CheckSongFinished()
	{
		// Check if the jukebox is in play mode and if the sound is finished playing.
		// We didn't use "AudioSource.isPlaying" here because of a racing condition between PlayNetworkAtPos latency and Update.

		// JESTER
		//if (SoundManager.Instance.IsSoundPlaying(soundSpawnToken))


		//if (IsPlaying && Time.time > startPlayTime + audioSource.clip.length)
		//{
		//	// The fun isn't over, we just finished the current track.  We just start playing the next one (or stop if it was the last one).
		//	if (!NextSong())
		//		Stop();
		//}
	}

	public async Task Play()
	{
		// Too much damage stops the jukebox from being able to play
		if (integrity.integrity > integrity.initialIntegrity / 2)
		{
			IsPlaying = true;
			spriteHandler.SetSpriteSO(SpritePlaying);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters
			{
				MixerType = MixerType.Muffled,
				SpatialBlend = 1, // 3D, we need it to attenuate with distance
				Volume = Volume,
				MinDistance = MinSoundDistance,
				MaxDistance = MaxSoundDistance,
				VolumeRolloffType = VolumeRolloffType.EaseInAndOut,
				Spread = Spread

			};

			AddressableAudioSource addressableAudioSourceFromCache = await GetAddressableAudioSourceFromCache();
			soundSpawnToken = await SoundManager.PlayNetworkedAtPos(addressableAudioSourceFromCache, registerTile.WorldPositionServer, audioSourceParameters, false, true, gameObject);

			startPlayTime = Time.time;
			UpdateGUIAsync();
		}
	}

	public void Stop()
	{
		IsPlaying = false;

		if (integrity.integrity >= integrity.initialIntegrity / 2)
			spriteHandler.SetSpriteSO(SpriteIdle);
		else
			spriteHandler.SetSpriteSO(SpriteDamaged);

		SoundManager.StopNetworked(soundSpawnToken);

		UpdateGUIAsync();
	}

	public void PreviousSong()
	{
		if (currentSongTrackIndex > 0)
		{
			if (IsPlaying)
				SoundManager.StopNetworked(soundSpawnToken);

			// JESTER
			// We unload the music from the library, freeing RAM
			//SoundManager.Instance.UnloadMusic(SoundManager.Instance.MusicLibrary.ElementAt(currentSongTrackIndex).Path);

			currentSongTrackIndex--;
			UpdateGUIAsync();

			if (IsPlaying)
				Play();
		}
	}

	public bool NextSong()
	{
		if (currentSongTrackIndex < SoundManager.Instance.MusicLibrary.Count - 1)
		{
			if (IsPlaying)
				SoundManager.StopNetworked(soundSpawnToken);

			// JESTER
			// We unload the music from the library, freeing RAM
			// SoundManager.Instance.UnloadMusic(SoundManager.Instance.MusicLibrary.ElementAt(currentSongTrackIndex).Path);

			currentSongTrackIndex++;
			UpdateGUIAsync();

			if (IsPlaying)
				Play();

			return true;
		}
		else
			return false;
	}

	public void VolumeChange(float newVolume)
	{
		Volume = newVolume;

		AudioSourceParameters audioSourceParameters = new AudioSourceParameters
		{
			Volume = newVolume,
		};

		ChangeAudioSourceParametersMessage.SendToAll(soundSpawnToken, audioSourceParameters);
	}

	private void OnDamageReceived(DamageInfo damageInfo)
	{
		if (integrity.integrity <= integrity.initialIntegrity / 2)
		{
			Stop();
		}
	}

	private async Task UpdateGUIAsync()
	{
		List<ElementValue> valuesToSend = new List<ElementValue>();
		valuesToSend.Add(new ElementValue() { Id = "TextTrack", Value = Encoding.UTF8.GetBytes(TrackPosition) });
		valuesToSend.Add(new ElementValue() { Id = "TextSong", Value = Encoding.UTF8.GetBytes(await GetSongNameAsync().ConfigureAwait(false)) });
		valuesToSend.Add(new ElementValue() { Id = "TextArtist", Value = Encoding.UTF8.GetBytes(await GetArtistNameAsync().ConfigureAwait(false)) });
		valuesToSend.Add(new ElementValue() { Id = "ImagePlayStop", Value = Encoding.UTF8.GetBytes(PlayStopButtonPrefabImage) });

		// Update all UI currently opened.
		TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Jukebox, TabAction.Update, valuesToSend.ToArray());
	}
}