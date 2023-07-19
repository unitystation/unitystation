using System.Collections;
using Mirror;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Systems.Electricity;
using AddressableReferences;
using Audio.Containers;
using Messages.Server;
using Messages.Server.SoundMessages;
using Systems.Explosions;

namespace Objects
{
	/// <summary>
	/// A machine that plays music choosen by it's user's tastes in a cool place like a lounge or a bar.
	/// </summary>
	public class Jukebox : NetworkBehaviour, IAPCPowerable
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

		private AudioSourceParameters audioSourceParameters;

		[SerializeField]
		private AudioClipsArray adminMusic = null;

		private List<AddressableAudioSource> musics;

		private List<string> guid = new List<string>();

		/// <summary>
		/// The current state of the jukebox powered/overpowered/underpowered/no power
		/// </summary>
		private PowerState CurrentState;

		/// <summary>
		/// The current state of the jukebox powered/overpowered/underpowered/no power
		/// </summary>
		private APCPoweredDevice APCConnectionHandler;

		private Integrity integrity;
		private APCPoweredDevice power;
		private RegisterTile registerTile;
		private int currentSongTrackIndex = 0;
		private float startPlayTime;
		private bool secondLoadAttempt;

		public bool IsPlaying { get; set; } = false;

		public string TrackPosition {
			get {
				return $"{currentSongTrackIndex + 1} / {musics.Count}";
			}
		}

		public string SongName {
			get {
				string songName = musics[currentSongTrackIndex].AudioSource.clip.name;
				return $"{songName.Split('_')[0]}";
			}
		}

		public string Artist {
			get {
				string songName = musics[currentSongTrackIndex].AudioSource.clip.name;
				string artist = songName.Contains("_") ? songName.Split('_')[1] : "Unknown";
				return $"{artist}";
			}
		}

		public string PlayStopButtonPrefabImage {
			get {
				return IsPlaying ? "GUI_Jukebox_Stop" : "GUI_Jukebox_Play";
			}
		}

		#region Lifecycle

		private void Awake()
		{
			APCConnectionHandler = GetComponent<APCPoweredDevice>();
			power = GetComponent<APCPoweredDevice>();
			registerTile = GetComponent<RegisterTile>();
			integrity = GetComponent<Integrity>();
			integrity.OnApplyDamage.AddListener(OnDamageReceived);

			audioSourceParameters = new AudioSourceParameters(volume: Volume, spatialBlend: 1, spread: Spread,
				minDistance: MinSoundDistance, maxDistance: MaxSoundDistance, mixerType: MixerType.JukeBox,
				volumeRolloffType: VolumeRolloffType.EaseInAndOut);
		}

		private void Start()
		{
			_ = InternalStart();
		}

		private async Task InternalStart()
		{
			// We want the same musics that are in the lobby,
			// so, I copy it's playlist here instead of managing two different playlists in UnityEditor.
			musics = new List<AddressableAudioSource>();

			foreach (var audioSource in adminMusic.AddressableAudioSource)
			{
				var song = await AudioManager.GetAddressableAudioSourceFromCache(new List<AddressableAudioSource> { audioSource });
				musics.Add(song);
			}

			UpdateGUI();
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		#endregion

		private void UpdateMe()
		{
			// Check if the jukebox is in play mode and if the sound is finished playing.
			// We didn't use "AudioSource.isPlaying" here because of a racing condition between PlayNetworkAtPos latency and Update.
			if (IsPlaying && Time.time > startPlayTime + musics[currentSongTrackIndex].AudioSource.clip.length)
			{
				NextSong();
			}
		}

		public async Task Play()
		{
			// Too much damage stops the jukebox from being able to play
			if (integrity.integrity < integrity.initialIntegrity / 2)
			{
				SparkUtil.TrySpark(gameObject);
				return;
			}

			await StopAllGuids();
			IsPlaying = true;
			StartCoroutine(UpdateSprites(SpritePlaying));
			guid.Add(await SoundManager.PlayNetworkedAtPosAsync(musics[currentSongTrackIndex], registerTile.WorldPositionServer, audioSourceParameters, false, true, sourceObj: gameObject));
			startPlayTime = Time.time;
			UpdateGUI();
		}

		public async Task Stop(bool autoplay = false)
		{
			if (autoplay == false) IsPlaying = false;
			if (integrity.integrity >= integrity.initialIntegrity / 2)
			{
				StartCoroutine(UpdateSprites(SpriteIdle));
			}
			else
			{
				StartCoroutine(UpdateSprites(SpriteDamaged));
			}
			await Task.Run(StopAllGuids);

			UpdateGUI();
		}

		private Task StopAllGuids()
		{
			foreach (var id in guid)
			{
				SoundManager.StopNetworked(id);
			}
			guid.Clear();

			return Task.CompletedTask;
		}

		public async void PreviousSong()
		{
			await Stop(true);
			if (currentSongTrackIndex <= 0) currentSongTrackIndex = musics.Count - 1;
			currentSongTrackIndex--;
			UpdateGUI();

			if (IsPlaying) _ = Play();
		}

		public async void NextSong()
		{
			await Stop(true);
			if (currentSongTrackIndex >= musics.Count - 1) currentSongTrackIndex = -1;
			currentSongTrackIndex++;
			UpdateGUI();

			if (IsPlaying) _ = Play();
		}

		public void VolumeChange(float newVolume)
		{
			audioSourceParameters.Volume = newVolume;

			audioSourceParameters.IsMute = newVolume <= 0;

			if(guid.Count != 0) ChangeAudioSourceParametersMessage.SendToAll(guid[0], audioSourceParameters);
		}

		private void OnDamageReceived(DamageInfo damageInfo)
		{
			if (integrity.integrity <= integrity.initialIntegrity / 2)
			{
				_ = Stop();
			}
		}

		private IEnumerator UpdateSprites(SpriteDataSO spriteDataSo)
		{
			spriteHandler.SetSpriteSO(spriteDataSo);
			yield return null;
		}

		private void UpdateGUI()
		{
			if (musics == null || musics.Count == 0)
			{
				if (secondLoadAttempt == false)
				{
					secondLoadAttempt = true;
					_ = InternalStart();
				}

				return;
			}

			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.Jukebox);
			if(peppers.Count == 0) return;

			List<ElementValue> valuesToSend = new List<ElementValue>();
			valuesToSend.Add(new ElementValue() { Id = "TextTrack", Value = Encoding.UTF8.GetBytes(TrackPosition) });
			valuesToSend.Add(new ElementValue() { Id = "TextSong", Value = Encoding.UTF8.GetBytes(SongName) });
			valuesToSend.Add(new ElementValue() { Id = "TextArtist", Value = Encoding.UTF8.GetBytes(Artist) });
			valuesToSend.Add(new ElementValue() { Id = "ImagePlayStop", Value = Encoding.UTF8.GetBytes(PlayStopButtonPrefabImage) });

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Jukebox, TabAction.Update, valuesToSend.ToArray());
		}

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage)
		{
			// Nothing really.  Only the state matters.  (See StateUpdate).
		}

		public void StateUpdate(PowerState State)
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
			APCConnectionHandler.Wattusage = IsPlaying? InUseWattUsage : StandByWattUsage;
		}

		#endregion
	}
}
