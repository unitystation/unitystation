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
		[SerializeField]
		private Slider camZoomSlider = null;
		[SerializeField]
		private Toggle scrollWheelZoomToggle = null;
		[SerializeField]
		private Toggle fullscreenToggle = null;
		[SerializeField]
		private Toggle vSyncToggle = null;
		[SerializeField]
		private GameObject vSyncWarning = null;

		private CameraZoomHandler zoomHandler;
		[SerializeField] private InputField frameRateTarget = null;
		[SerializeField]
		private Slider chatBubbleSizeSlider = null;

		void OnEnable()
		{
			if (zoomHandler == null)
			{
				zoomHandler = FindObjectOfType<CameraZoomHandler>();
			}

			Refresh();
		}

		void Refresh()
		{
			if (!PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleSize))
			{
				PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, 2f);
				PlayerPrefs.Save();
			}
			if (!PlayerPrefs.HasKey(PlayerPrefKeys.EnableVSync))
			{
				PlayerPrefs.SetInt(PlayerPrefKeys.EnableVSync, 1);
				PlayerPrefs.Save();
			}

			int vSync = PlayerPrefs.GetInt(PlayerPrefKeys.EnableVSync);
			vSyncToggle.isOn = vSync != 0;
			vSyncWarning.SetActive(vSyncToggle.isOn);
			QualitySettings.vSyncCount = vSync;

			chatBubbleSizeSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.ChatBubbleSize);
			camZoomSlider.value = zoomHandler.ZoomLevel / 8;
			scrollWheelZoomToggle.isOn = zoomHandler.ScrollWheelZoom;
			frameRateTarget.text = PlayerPrefs.GetInt(PlayerPrefKeys.TargetFrameRate).ToString();
		}

		public void OnZoomLevelChange()
		{
			zoomHandler.SetZoomLevel((int)camZoomSlider.value * 8);
			Refresh();
		}

		public void OnChatBubbleSizeChange()
		{
			PlayerPrefs.SetFloat(PlayerPrefKeys.ChatBubbleSize, chatBubbleSizeSlider.value);
			PlayerPrefs.Save();
			Refresh();
		}

		public void OnScrollWheelToggle()
		{
			zoomHandler.ToggleScrollWheelZoom(scrollWheelZoomToggle.isOn);
			Refresh();
		}

		/// <summary>
		/// Toggles fullscreen. Fullscreen uses native resolution, windowed always uses 720p.
		/// </summary>
		public void OnFullscreenToggle()
		{
			int hRes = Screen.fullScreen ? 1280 : Display.main.systemWidth;
			int vRes = Screen.fullScreen ? 720 : Display.main.systemHeight;
			fullscreenToggle.isOn = !Screen.fullScreen; //This can't go into Refresh() because going fullscreen happens 1 too late
			Screen.SetResolution(hRes, vRes, !Screen.fullScreen);
		}

		/// <summary>
		/// Toggles Vsync. Off or Every VBlank.
		/// </summary>
		public void OnVSyncToggle()
		{
			int vSync = vSyncToggle.isOn ? 1 : 0;
			vSyncWarning.SetActive(vSyncToggle.isOn);
			QualitySettings.vSyncCount = vSync;
			PlayerPrefs.SetInt(PlayerPrefKeys.EnableVSync, vSync);
			PlayerPrefs.Save();
		}

		public void ResetDefaults()
		{
			ModalPanelManager.Instance.Confirm(
				"Are you sure?",
				() =>
				{
					zoomHandler.ResetDefaults();
					Refresh();
				},
				"Reset"
			);
		}

		public void OnFrameRateTargetEdit()
		{
			int newTarget = 99;
			int.TryParse(frameRateTarget.text, out newTarget);
			PlayerPrefs.SetInt(PlayerPrefKeys.TargetFrameRate, newTarget);
			PlayerPrefs.Save();
			Application.targetFrameRate = Mathf.Clamp(newTarget, 30, 144);
		}
	}
}