using Assets.Scripts.Messages.Server.SoundMessages;
using Mirror;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Systems.Electricity;
using AddressableReferences;

namespace Objects
{
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

		private List<AudioSource> musics;

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

		public bool IsPlaying { get; set; } = false;

		public string TrackPosition {
			get {
				return $"{currentSongTrackIndex + 1} / {musics.Count}";
			}
		}

		public string SongName {
			get {
				string songName = musics[currentSongTrackIndex].clip.name;
				return $"{songName.Split('_')[0]}";
			}
		}

		public string Artist {
			get {
				string songName = musics[currentSongTrackIndex].clip.name;
				string artist = songName.Contains("_") ? songName.Split('_')[1] : "Unknown";
				return $"{artist}";
			}
		}

		public string PlayStopButtonPrefabImage {
			get {
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
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			APCConnectionHandler = GetComponent<APCPoweredDevice>();
			power = GetComponent<APCPoweredDevice>();
			registerTile = GetComponent<RegisterTile>();
			musics = new List<AudioSource>();

			Transform transformAdminMusic = SoundManager.Instance.transform.Find("AdminMusic");

			if (transformAdminMusic == null)
			{
				Logger.LogWarning("Update Jukebox to sound addressables.");
				return; // stop NRE in foreach
			}

			foreach (Transform transform in transformAdminMusic)
			{
				musics.Add(transform.GetComponent<AudioSource>());
			}

			UpdateGUI();
		}

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
			integrity.OnApplyDamage.AddListener(OnDamageReceived);
		}

		private void Update()
		{
			// Check if the jukebox is in play mode and if the sound is finished playing.
			// We didn't use "AudioSource.isPlaying" here because of a racing condition between PlayNetworkAtPos latency and Update.
			if (IsPlaying && Time.time > startPlayTime + musics[currentSongTrackIndex].clip.length)
			{
				// The fun isn't over, we just finished the current track.  We just start playing the next one (or stop if it was the last one).
				if (!NextSong())
					Stop();
			}
		}

		public void Play()
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

				SoundManager.PlayNetworkedAtPos(musics[currentSongTrackIndex].name, registerTile.WorldPositionServer, audioSourceParameters, false, true, gameObject);
				startPlayTime = Time.time;
				UpdateGUI();
			}
		}

		public void Stop()
		{
			IsPlaying = false;

			if (integrity.integrity >= integrity.initialIntegrity / 2)
				spriteHandler.SetSpriteSO(SpriteIdle);
			else
				spriteHandler.SetSpriteSO(SpriteDamaged);

			SoundManager.StopNetworked(musics[currentSongTrackIndex].name);

			UpdateGUI();
		}

		public void PreviousSong()
		{
			if (currentSongTrackIndex > 0)
			{
				if (IsPlaying)
					SoundManager.StopNetworked(musics[currentSongTrackIndex].name);

				currentSongTrackIndex--;
				UpdateGUI();

				if (IsPlaying)
					Play();
			}
		}

		public bool NextSong()
		{
			if (currentSongTrackIndex < musics.Count - 1)
			{
				if (IsPlaying)
					SoundManager.StopNetworked(musics[currentSongTrackIndex].name);

				currentSongTrackIndex++;
				UpdateGUI();

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

			ChangeAudioSourceParametersMessage.SendToAll(musics[currentSongTrackIndex].name, audioSourceParameters);
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
}
