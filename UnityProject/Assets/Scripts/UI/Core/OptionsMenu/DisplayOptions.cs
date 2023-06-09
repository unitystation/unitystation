using System.Collections;
using System.Collections.Generic;
using Managers.SettingsManager;
using UI.Core;
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
		[SerializeField] private Slider uiScaleSlider = null;

		[SerializeField] private Toggle scrollWheelZoomToggle = null;

		void OnEnable()
		{
			DisplaySettings.Instance.SettingsChanged += DisplaySettings_SettingsChanged;
			RefreshForm();
		}

		private void OnDisable()
		{
			DisplaySettings.Instance.SettingsChanged -= DisplaySettings_SettingsChanged;
		}

		private void DisplaySettings_SettingsChanged(object sender, DisplaySettings.DisplaySettingsChangedEventArgs e)
		{
			RefreshForm();
		}

		/// <summary>
		/// Update the form to match currently used values
		/// </summary>
		void RefreshForm()
		{
			fullscreenToggle.isOn = DisplaySettings.Instance.IsFullScreen;

			bool vSync = DisplaySettings.Instance.VSyncEnabled;
			vSyncToggle.isOn = vSync;
			vSyncWarning.SetActive(vSync);

			frameRateTarget.text = DisplaySettings.Instance.TargetFrameRate.ToString();
			frameRateTarget.textComponent.color = VALIDCOLOR;

			camZoomSlider.value = DisplaySettings.Instance.ZoomLevel / 8f;

			scrollWheelZoomToggle.isOn = DisplaySettings.Instance.ScrollWheelZoom;
			uiScaleSlider.value = PlayerPrefs.GetFloat(DisplaySettings.UISCALE_KEY, DisplaySettings.UISCALE_DEFAULT);
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
					DisplaySettings.Instance.SetPrefDefaults();
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

			DisplaySettings.Instance.SetFullscreen(fullscreenToggle.isOn);
		}

		/// <summary>
		/// Sets a new VSync setting and shows the TargetFrameRate warning if enabled
		/// </summary>
		public void OnVSyncToggle()
		{
			DisplaySettings.Instance.VSyncEnabled = vSyncToggle.isOn;
			vSyncWarning.SetActive(vSyncToggle.isOn);
		}

		/// <summary>
		/// Validates FrameRateTarget input, indicated with colored text. Use the new value if valid.
		/// </summary>
		public void OnFrameRateTargetEdit()
		{
			if (int.TryParse(frameRateTarget.text, out int newTarget) &&
			    newTarget >= DisplaySettings.Instance.Min_TargetFrameRate &&
			    newTarget <= DisplaySettings.Instance.Max_TargetFrameRate)
			{
				frameRateTarget.textComponent.color = VALIDCOLOR;
				DisplaySettings.Instance.TargetFrameRate = newTarget;
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
			DisplaySettings.Instance.ZoomLevel = value;
		}

		public void OnUIScaleChange()
		{
			UIManager.Instance.Scaler.scaleFactor = uiScaleSlider.value;
			PlayerPrefs.SetFloat(DisplaySettings.UISCALE_KEY, uiScaleSlider.value);
			PlayerPrefs.Save();
		}

		public void OnScrollWheelToggle()
		{
			DisplaySettings.Instance.ScrollWheelZoom = scrollWheelZoomToggle.isOn;
		}
	}
}