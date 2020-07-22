using Audio.Containers;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
	private Sprite SpriteIdle;

	[SerializeField]
	private SpriteSheetAndData SpritePlaying = null;

	[SerializeField]
	private SpriteSheetAndData SpriteDamaged;

	[SerializeField] private AudioClipsArray audioClips = null;

	private SpriteRenderer spriteRenderer;

	/// <summary>
	/// The current state of the jukebox powered/overpowered/underpowered/no power
	/// </summary>
	[HideInInspector] public PowerStates CurrentState;

	/// <summary>
	/// The current state of the jukebox powered/overpowered/underpowered/no power
	/// </summary>
	[HideInInspector] private APCPoweredDevice APCConnectionHandler;

	private AudioSource audioSource;

	private Integrity integrity;
	private Jukebox jukebox;
	private APCPoweredDevice power;
	private int currentSongTrackIndex = 0;

	public bool IsPlaying { get; set; } = false;


	public string TrackPosition
	{
		get
		{
			return $"Track {currentSongTrackIndex + 1} / {audioClips.AudioClips.Length}";
		}
	}

	public string SongName
	{
		get
		{
			string songName = audioClips.AudioClips[currentSongTrackIndex].name;
			return $"Song : {songName.Split('_')[0]}";
		}
	}

	public string Artist
	{
		get
		{
			string songName = audioClips.AudioClips[currentSongTrackIndex].name;
			string artist = songName.Contains("_") ? songName.Split('_')[1] : "Unknown";
			return $"Artist : {artist}";
		}
	}

	public int CurrentTrackIndex
	{
		get
		{
			return currentSongTrackIndex;
		}
	}

	public int TotalTrackCount
	{
		get
		{
			return audioClips.AudioClips.Length;
		}
	}

	public string CurrentSong
	{
		get
		{
			return audioClips.AudioClips[currentSongTrackIndex].name;
		}
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
		if (audioSource != null && audioSource.isPlaying)
		{
			APCConnectionHandler.Wattusage = InUseWattUsage;
		}
		else
		{
			APCConnectionHandler.Wattusage = StandByWattUsage;
		}
	}

	// Start is called before the first frame update
	void Start()
	{
		// We want the same musics that are in the lobby,
		// so, I copy it's playlist here instead of managing two different playlists in UnityEditor.
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		APCConnectionHandler = GetComponent<APCPoweredDevice>();
		jukebox = GetComponent<Jukebox>();
		power = GetComponent<APCPoweredDevice>();

		audioSource = GetComponent<AudioSource>();
		audioSource.volume = 1;
		UpdateGUI();
	}

	void Awake()
	{
		integrity = GetComponent<Integrity>();
		integrity.OnApplyDamage.AddListener(OnDamageReceived);
	}

	void Update()
	{
		if (IsPlaying && !audioSource.isPlaying)
		{
			// The fun isn't over, we just finished the current track.  We just start playing the next one.
			NextSong();
		}
	}

	public void Play()
	{
		// Too much damage stops the jukebox from being able to play
		if (integrity.integrity > integrity.initialIntegrity / 2)
		{
			IsPlaying = true;
			spriteHandler.SetSprite(SpritePlaying);
			audioSource.clip = audioClips.AudioClips[currentSongTrackIndex];
			audioSource.Play();
			UpdateGUI();
		}
	}

	public void Stop()
	{
		IsPlaying = false;

		if (integrity.integrity >= integrity.initialIntegrity / 2)
			spriteHandler.SetSprite(SpriteIdle);
		else
			spriteHandler.SetSprite(SpriteDamaged);

		audioSource.Stop();
		UpdateGUI();
	}

	public void PreviousSong()
	{
		if (currentSongTrackIndex > 0)
		{
			currentSongTrackIndex--;
			audioSource.clip = audioClips.AudioClips[currentSongTrackIndex];
			UpdateGUI();

			if (IsPlaying)
				audioSource.Play();
		}
	}

	public void NextSong()
	{
		if (currentSongTrackIndex < audioClips.AudioClips.Length - 1)
		{
			currentSongTrackIndex++;
			audioSource.clip = audioClips.AudioClips[currentSongTrackIndex];
			UpdateGUI();

			if (IsPlaying)
				audioSource.Play();
		}
	}

	private void OnDamageReceived(DamageInfo damageInfo)
	{
		if (integrity.integrity <= integrity.initialIntegrity / 2)
		{
			Stop();
		}
	}

	private void UpdateGUI()
	{
		List<ElementValue> valuesToSend = new List<ElementValue>();
		valuesToSend.Add(new ElementValue() { Id = "TextTrack", Value = Encoding.UTF8.GetBytes(TrackPosition) });
		valuesToSend.Add(new ElementValue() { Id = "TextSong", Value = Encoding.UTF8.GetBytes(SongName) });
		valuesToSend.Add(new ElementValue() { Id = "TextArtist", Value = Encoding.UTF8.GetBytes(Artist) });


		valuesToSend.Add(new ElementValue() { Id = "ImagePlayStop", Value = Encoding.UTF8.GetBytes(PlayStopButtonPrefabImage) });

		// Update all UI currently opened.
		TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Jukebox, TabAction.Update, valuesToSend.ToArray());
	}
}