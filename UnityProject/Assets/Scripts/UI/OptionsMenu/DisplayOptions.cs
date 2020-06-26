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

		[SerializeField] private Toggle fullscreenToggle = null;

		[SerializeField] private Toggle vSyncToggle = null;
		[SerializeField] private GameObject vSyncWarning = null;

		[SerializeField] private InputField frameRateTarget = null;

		[SerializeField] private Slider camZoomSlider = null;

		[SerializeField] private Toggle scrollWheelZoomToggle = null;

		[SerializeField] private Slider chatBubbleSizeSlider = null;

		private DisplaySettings displaySettings = null;

		void OnEnable()
		{
			displaySettings.SettingsChanged += DisplaySettings_SettingsChanged;
			RefreshForm();
		}

		private void OnDisable()
		{
			displaySettings.SettingsChanged -= DisplaySettings_SettingsChanged;
		}

		private void DisplaySettings_SettingsChanged(object sender, DisplaySettings.DisplaySettingsChangedEventArgs e)
		{
			RefreshForm();
		}

		private void Awake()
		{
			displaySettings = FindObjectOfType<DisplaySettings>();
		}

		/// <summary>
		/// Update the form to match currently used values
		/// </summary>
		void RefreshForm()
		{
			fullscreenToggle.isOn = displaySettings.IsFullScreen;

			bool vSync = displaySettings.VSyncEnabled;
			vSyncToggle.isOn = vSync;
			vSyncWarning.SetActive(vSync);

			frameRateTarget.text = displaySettings.TargetFrameRate.ToString();
			frameRateTarget.textComponent.color = VALIDCOLOR;

			camZoomSlider.value = displaySettings.ZoomLevel / 8;

			scrollWheelZoomToggle.isOn = displaySettings.ScrollWheelZoom;

			chatBubbleSizeSlider.value = displaySettings.ChatBubbleSize;
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
					displaySettings.SetPrefDefaults();
					RefreshForm();
				},
				"Reset"
			);
		}

		/// <summary>
		/// Toggles fullscreen. Fullscreen uses native resolution, windowed uses a default resolution.
		/// </summary>
		public void OnFullscreenToggle()
		{
			// Ensure this togglebox isn't triggered by attempts to fullscreen with Alt-Enter
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				if (Input.GetKey(KeyCode.LeftAlt))
				{
					return;
				}
			}

			displaySettings.SetFullscreen(fullscreenToggle.isOn);
		}

		/// <summary>
		/// Sets a new VSync setting and shows the TargetFrameRate warning if enabled
		/// </summary>
		public void OnVSyncToggle()
		{
			displaySettings.VSyncEnabled = vSyncToggle.isOn;
			vSyncWarning.SetActive(vSyncToggle.isOn);
		}

		/// <summary>
		/// Validates FrameRateTarget input, indicated with colored text. Use the new value if valid.
		/// </summary>
		public void OnFrameRateTargetEdit()
		{
			if (int.TryParse(frameRateTarget.text, out int newTarget) && newTarget >= displaySettings.Min_TargetFrameRate && newTarget <= displaySettings.Max_TargetFrameRate)
			{
				frameRateTarget.textComponent.color = VALIDCOLOR;
				displaySettings.TargetFrameRate = newTarget;
			}
			else
			{
				frameRateTarget.textComponent.color = INVALIDCOLOR;
				return;
			}
		}

		public void OnZoomLevelChange()
		{
			int value = (int)camZoomSlider.value * 8;
			displaySettings.ZoomLevel = value;
		}

		public void OnScrollWheelToggle()
		{
			displaySettings.ScrollWheelZoom = scrollWheelZoomToggle.isOn;
		}

		public void OnChatBubbleSizeChange()
		{
			displaySettings.ChatBubbleSize = chatBubbleSizeSlider.value;
		}
	}
}