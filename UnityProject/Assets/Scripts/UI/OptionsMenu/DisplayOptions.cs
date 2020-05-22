using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unitystation.Options
{
	/// <summary>
	/// Controller for the display options view
	/// <summary>
	public class DisplayOptions : MonoBehaviour
	{
		private Color VALIDCOLOR = Color.white;
		private Color INVALIDCOLOR = Color.red;

		private const int DEFAULT_WINDOWWIDTH = 1280;
		private const int DEFAULT_WINDOWHEIGHT = 720;

		[SerializeField] private Toggle fullscreenToggle = null;

		[SerializeField] private Toggle vSyncToggle = null;
		[SerializeField] private GameObject vSyncWarning = null;
		private const int DEFAULT_VSYNCENABLED = 1;

		[SerializeField] private InputField frameRateTarget = null;
		private const int DEFAULT_TARGETFRAMERATE = 99;
		private const int MIN_TARGETFRAMERATE = 30;
		private const int MAX_TARGETFRAMERATE = 144;

		[SerializeField] private Slider camZoomSlider = null;
		private CameraZoomHandler zoomHandler;

		[SerializeField] private Toggle scrollWheelZoomToggle = null;

		[SerializeField] private Slider chatBubbleSizeSlider = null;
		private const float DEFAULT_CHATBUBBLESIZE = 2f;

		void OnEnable()
		{
			RefreshForm();
		}

		private void Awake()
		{
			InitSettings();
		}

		private bool init = false;
		/// <summary>
		/// Set up this object, load and set up Display related PlayerPrefs,
		/// then apply the settings to the current session
		/// </summary>
		public void InitSettings()
		{
			if (!init)
			{
				init = true;
				if (zoomHandler == null)
				{
					zoomHandler = FindObjectOfType<CameraZoomHandler>();
				}

				SetupPrefs();
			}
		}

		/// <summary>
		/// Load and set up Display related PlayerPrefs,
		/// then apply the settings to the current session
		/// </summary>
		void SetupPrefs()
		{
			if (!PlayerPrefs.HasKey(PlayerPrefKeys.EnableVSync))
			{
				SetPrefDefaults(PlayerPrefKeys.EnableVSync);
			}
			else
			{
				ApplyEnableVSync();
			}

			if (!PlayerPrefs.HasKey(PlayerPrefKeys.TargetFrameRate))
			{
				SetPrefDefaults(PlayerPrefKeys.TargetFrameRate);
			}
			else
			{
				ApplyTargetFrameRate();
			}

			if (!PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize))
			{
				SetPrefDefaults(PlayerPrefKeys.ChatBubbleSize);
			}
			// else Apply not required, just setting the pref is enough

			RefreshForm();
		}

		/// <summary>
		/// Update the form to match currently used values
		/// </summary>
		void RefreshForm()
		{
			fullscreenToggle.isOn = Screen.fullScreen;

			int vSync = PlayerPrefs.GetInt(PlayerPrefKeys.EnableVSync);
			vSyncToggle.isOn = vSync != 0;
			vSyncWarning.SetActive(vSyncToggle.isOn);

			frameRateTarget.text = PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate).ToString();
			frameRateTarget.textComponent.color = VALIDCOLOR;

			camZoomSlider.value = zoomHandler.ZoomLevel / 8;

			scrollWheelZoomToggle.isOn = zoomHandler.ScrollWheelZoom;

			chatBubbleSizeSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleSize);
		}

		/// <summary>
		/// Reset DisplayOptions settings to their default values and apply them
		/// </summary>
		public void ResetDefaults()
		{
			ModalPanelManager.Instance.Confirm(
				"Are you sure?",
				() =>
				{
					zoomHandler.ResetDefaults();
					SetPrefDefaults();
					// Skipping resetting fullscreen state
					RefreshForm();
				},
				"Reset"
			);
		}

		/// <summary>
		/// Resets all Display playerPrefs to defaults, alternatively provide specific PlayerPrefKeys entries to only reset those. 
		/// </summary>
		/// <param name="playerPrefKeysEntries">Set of PlayerPrefKeys strings associated with a setting, to reset</param>
		void SetPrefDefaults(params string[] playerPrefKeysEntries)
		{
			bool resetEnableVSync;
			bool resetTargetFrameRate;
			bool resetChatBubbleSize;

			if (playerPrefKeysEntries.Length == 0)
			{ // Set every display pref to be reset
				resetEnableVSync = true;
				resetTargetFrameRate = true;
				resetChatBubbleSize = true;
			}
			else
			{ // Reset any display pref listed in the params
				List<string> prefEntries = new List<string>(playerPrefKeysEntries);

				resetEnableVSync = prefEntries.Contains(PlayerPrefKeys.EnableVSync);
				resetTargetFrameRate = prefEntries.Contains(PlayerPrefKeys.TargetFrameRate);
				resetChatBubbleSize = prefEntries.Contains(PlayerPrefKeys.ChatBubbleSize);
			}

			if (resetEnableVSync)
			{
				SetEnableVSync(DEFAULT_VSYNCENABLED);
			}
			if (resetTargetFrameRate)
			{
				SetTargetFrameRate(DEFAULT_TARGETFRAMERATE);
			}
			if (resetChatBubbleSize)
			{
				SetChatBubbleSize(DEFAULT_CHATBUBBLESIZE);
			}

			RefreshForm();
		}

		/// <summary>
		/// Set new EnableVSync PlayerPref and apply the change
		/// </summary>
		/// <param name="newValue">0 - Off, 1 - Every VBlank</param>
		void SetEnableVSync(int newValue)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.EnableVSync, newValue);
			PlayerPrefs.Save();
			ApplyEnableVSync();
		}

		/// <summary>
		/// Apply the VSync setting from PlayerPrefs
		/// </summary>
		void ApplyEnableVSync()
		{
			int vSync = PlayerPrefs.GetInt(PlayerPrefKeys.EnableVSync);
			QualitySettings.vSyncCount = vSync;
		}

		/// <summary>
		/// Set new TargetFrameRate PlayerPref and apply the change
		/// </summary>
		/// <param name="newValue">Target frame rate/></param>
		void SetTargetFrameRate(int newValue)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, newValue);
			PlayerPrefs.Save();
			ApplyTargetFrameRate();
		}

		/// <summary>
		/// Apply the TargetFrameRate setting from PlayerPrefs
		/// </summary>
		void ApplyTargetFrameRate()
		{
			int target = PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate);
			Application.targetFrameRate = Mathf.Clamp(target, MIN_TARGETFRAMERATE, MAX_TARGETFRAMERATE);
		}

		/// <summary>
		/// Set new ChatBubbleSize PlayerPref
		/// </summary>
		/// <param name="newValue">New chat bubble size/></param>
		void SetChatBubbleSize(float newValue)
		{
			PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, newValue);
			PlayerPrefs.Save();
			// Apply not required, just setting the pref is enough
		}

		/// <summary>
		/// Toggles fullscreen. Fullscreen uses native resolution, windowed uses a default resolution.
		/// </summary>
		public void OnFullscreenToggle()
		{
			if (fullscreenToggle.isOn != Screen.fullScreen)
			{ // If the window fullscreen state was changed outside of this menu we won't do this
				int hRes = fullscreenToggle.isOn ? Display.main.systemWidth : DEFAULT_WINDOWWIDTH;
				int vRes = fullscreenToggle.isOn ? Display.main.systemHeight : DEFAULT_WINDOWHEIGHT;
				Screen.SetResolution(hRes, vRes, fullscreenToggle.isOn);
			}
		}

		/// <summary>
		/// Sets a new VSync setting and shows the TargetFrameRate warning if enabled
		/// </summary>
		public void OnVSyncToggle()
		{
			int vSync = vSyncToggle.isOn ? 1 : 0;
			SetEnableVSync(vSync);
			vSyncWarning.SetActive(vSyncToggle.isOn);
		}

		/// <summary>
		/// Validates FrameRateTarget input, indicated with colored text. Use the new value if valid.
		/// </summary>
		public void OnFrameRateTargetEdit()
		{
			if (int.TryParse(frameRateTarget.text, out int newTarget) && newTarget >= MIN_TARGETFRAMERATE && newTarget <= MAX_TARGETFRAMERATE)
			{
				frameRateTarget.textComponent.color = VALIDCOLOR;
				SetTargetFrameRate(newTarget);
			}
			else
			{
				frameRateTarget.textComponent.color = INVALIDCOLOR;
				return;
			}
		}

		public void OnZoomLevelChange()
		{
			zoomHandler.SetZoomLevel((int)camZoomSlider.value * 8);
		}

		public void OnScrollWheelToggle()
		{
			zoomHandler.SetScrollWheelZoom(scrollWheelZoomToggle.isOn);
		}

		public void OnChatBubbleSizeChange()
		{
			SetChatBubbleSize(chatBubbleSizeSlider.value);
		}
	}
}