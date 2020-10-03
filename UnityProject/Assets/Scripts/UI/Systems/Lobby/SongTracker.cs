using UnityEngine;
using UnityEngine.UI;

namespace Audio.Containers
{
	/// <summary>
	/// Class used to display the information of the song being played in the lobby screen.
	/// </summary>
	public class SongTracker : MonoBehaviour
	{
		[SerializeField] private Slider volumeSlider = null;
		[SerializeField] private Button trackButton = null;
		[SerializeField] private Text trackName = null;
		[SerializeField] private Text artist = null;
		[SerializeField] private Image speakerImage = null;
		[SerializeField] private Sprite speakerOn = null;
		[SerializeField] private Sprite speakerOff = null;
		[SerializeField] private Color onColor = new Color(178, 194, 204); // Pale Blue
		[SerializeField] private Color offColor = new Color(176, 176, 176); // Grey

		private float timeBetweenSongs = 2f;
		private float currentWaitTime = 0f;

		/// <summary>
		/// If true the SongTracker will continue to play tracks one after
		/// another in a random order
		/// </summary>
		public bool PlayingRandomPlayList { get; private set; }

		private void Awake()
		{
			ToggleUI(false);
			if (!PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic))
			{
				PlayerPrefs.SetInt(PlayerPrefKeys.MuteMusic, 1);
				PlayerPrefs.Save();
			}

			if (PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolume))
			{
				volumeSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolume);
			}
			else
			{
				volumeSlider.value = 0.5f;
			}
		}

		private void Start()
		{
			DetermineMuteState();
		}

		private void Update()
		{
			if (!PlayingRandomPlayList || CustomNetworkManager.isHeadless) return;

			if (MusicManager.isLobbyMusicPlaying()) return;

			currentWaitTime += Time.deltaTime;
			if (currentWaitTime >= timeBetweenSongs)
			{
				currentWaitTime = 0f;
				PlayRandomTrack();
			}

			DetermineMuteState();
		}

		public void StartPlayingRandomPlaylist()
		{
			if (CustomNetworkManager.isHeadless) return;

			PlayingRandomPlayList = true;
			PlayRandomTrack();
			ToggleUI(true);
		}

		public void Stop()
		{
			PlayingRandomPlayList = false;
			ToggleUI(false);
			MusicManager.StopMusic();
		}

		private void ToggleUI(bool isActive)
		{
			volumeSlider.gameObject.SetActive(isActive);
			trackButton.gameObject.SetActive(isActive);
			trackName.gameObject.SetActive(isActive);
			artist.gameObject.SetActive(isActive);
			speakerImage.gameObject.SetActive(isActive);
		}

		public void ToggleMusicMute()
		{
			var toggle = PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic);
			if (toggle == 0)
			{
				toggle = 1;
			}
			else
			{
				toggle = 0;
			}

			PlayerPrefs.SetInt(PlayerPrefKeys.MuteMusic, toggle);
			PlayerPrefs.Save();
			DetermineMuteState();

		}

		private void DetermineMuteState()
		{
			var toggle = PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic);
			switch (toggle)
			{
				case 0:
					speakerImage.sprite = speakerOff;
					speakerImage.color = offColor;
					MusicManager.Instance.ToggleMusicMute(true);
					break;
				case 1:
					speakerImage.sprite = speakerOn;
					speakerImage.color = onColor;
					MusicManager.Instance.ToggleMusicMute(false);
					break;
			}
		}

		private void PlayRandomTrack()
		{
			if (CustomNetworkManager.isHeadless) return;

			var songInfo = MusicManager.Instance.PlayRandomTrack();
			trackName.text = songInfo[0];
			// If the name of the artist is included, add it as well
			if (songInfo.Length == 2)
			{
				artist.text = songInfo[1];
			}
			else
			{
				artist.text = "";
			}
		}

		public void OnVolumeSliderChange()
		{
			MusicManager.Instance.ChangeVolume(volumeSlider.value);
		}

		public void OnTrackButtonClick()
		{
			PlayRandomTrack();
		}
	}
}
