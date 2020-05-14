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
			int vRes = Screen.fullScreen ? 720  : Display.main.systemHeight;
			fullscreenToggle.isOn = !Screen.fullScreen; //This can't go into Refresh() because going fullscreen happens 1 too late
			Screen.SetResolution(hRes, vRes, !Screen.fullScreen);

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