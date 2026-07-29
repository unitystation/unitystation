using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.Managers.LobbyManager;
using US13.Managers.UpdateManager;
using US13.PlayerPrefs;

namespace US13.UI.Systems.Lobby
{
	public class GUI_MenuMusicPlayer : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text trackLabel = default;
		[SerializeField]
		private Button previousButton = default;
		[SerializeField]
		private Button pauseButton = default;
		[SerializeField]
		private Button nextButton = default;
		[SerializeField]
		private Button muteButton = default;
		[SerializeField]
		private Slider volumeSlider = default;

		[SerializeField]
		private Image pauseIcon = default;
		[SerializeField]
		private Sprite pauseSprite = default;
		[SerializeField]
		private Sprite playSprite = default;

		[SerializeField]
		private Image speakerIcon = default;
		[SerializeField]
		private Sprite speakerOnSprite = default;
		[SerializeField]
		private Sprite speakerOffSprite = default;

		private string shownTrack;
		private MusicManager.PlaybackState shownState = MusicManager.PlaybackState.Stopped;

		private void Awake()
		{
			previousButton.onClick.AddListener(OnPreviousBtn);
			pauseButton.onClick.AddListener(OnPauseBtn);
			nextButton.onClick.AddListener(OnNextBtn);
			muteButton.onClick.AddListener(OnMuteBtn);
			volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
		}

		private void Start()
		{
			InitMutePref();
			InitVolumeSlider();
			RefreshMuteIcon();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			RefreshTrackLabel();
			RefreshPauseIcon();
		}

		private void RefreshTrackLabel()
		{
			var info = MusicManager.Instance.CurrentTrackInfo;
			if (info == null) return;
			if (info[0] == shownTrack) return;

			shownTrack = info[0];
			if (info.Length >= 2)
			{
				trackLabel.text = $"{info[0]} - {info[1]}";
				return;
			}
			trackLabel.text = info[0];
		}

		private void RefreshPauseIcon()
		{
			var state = MusicManager.Instance.State;
			if (state == shownState) return;

			shownState = state;
			if (state == MusicManager.PlaybackState.Paused)
			{
				pauseIcon.sprite = playSprite;
				return;
			}
			pauseIcon.sprite = pauseSprite;
		}

		private void OnPreviousBtn()
		{
			_ = MusicManager.Instance.PlayPreviousTrack();
		}

		private void OnNextBtn()
		{
			_ = MusicManager.Instance.PlayRandomTrack();
		}

		private void OnPauseBtn()
		{
			if (MusicManager.Instance.State == MusicManager.PlaybackState.Paused)
			{
				MusicManager.Instance.ResumeMusic();
				return;
			}
			MusicManager.Instance.PauseMusic();
		}

		private void OnMuteBtn()
		{
			ToggleMutePref();
			ApplyMuteState();
		}

		private void OnVolumeChanged(float value)
		{
			MusicManager.Instance.ChangeVolume(value);
		}

		private void InitMutePref()
		{
			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.MuteMusic)) return;

			UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.MuteMusic, 1);
			UnityEngine.PlayerPrefs.Save();
		}

		private void InitVolumeSlider()
		{
			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.MusicVolumeKey))
			{
				volumeSlider.value = UnityEngine.PlayerPrefs.GetFloat(PlayerPrefKeys.MusicVolumeKey);
				return;
			}
			volumeSlider.value = 0.8f;
		}

		private bool IsMutedPref()
		{
			return UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.MuteMusic) == 0;
		}

		private void ToggleMutePref()
		{
			var muted = IsMutedPref();
			UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.MuteMusic, muted ? 1 : 0);
			UnityEngine.PlayerPrefs.Save();
		}

		private void ApplyMuteState()
		{
			MusicManager.Instance.ToggleMusicMute(IsMutedPref());
			RefreshMuteIcon();
		}

		private void RefreshMuteIcon()
		{
			if (IsMutedPref())
			{
				speakerIcon.sprite = speakerOffSprite;
				return;
			}
			speakerIcon.sprite = speakerOnSprite;
		}
	}
}
