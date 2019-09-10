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
        private Slider camZoomSlider;
        [SerializeField]
        private Toggle scrollWheelZoomToggle;
        private CameraZoomHandler zoomHandler;

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
            camZoomSlider.value = zoomHandler.ZoomLevel;
            scrollWheelZoomToggle.isOn = zoomHandler.ScrollWheelZoom;
        }

        public void OnZoomLevelChange()
        {
            zoomHandler.SetZoomLevel(camZoomSlider.value);
            Refresh();
        }

        public void OnScrollWheelToggle()
        {
            zoomHandler.ToggleScrollWheelZoom(scrollWheelZoomToggle.isOn);
            Refresh();
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
    }
}