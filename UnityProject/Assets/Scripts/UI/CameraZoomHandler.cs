using System;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{	
    public class CameraZoomHandler : MonoBehaviour
    {
        //Reference to zoom text.
        public Text zoomText;
        private int zoomLevel = 1;
        private string zoomString = "1";

		void Start(){
			if (PlayerPrefs.HasKey("CamZoom")){
				zoomLevel = PlayerPrefs.GetInt("CamZoom");
				Refresh();
			} else {
				PlayerPrefs.SetInt("CamZoom", 1);
				PlayerPrefs.Save();
			}
		}

        // Refreshes after setting zoom level.
        public void Refresh()
        {          
            // Calculate ratio.
            double ratio = Camera.main.pixelHeight / (double) Camera.main.pixelWidth;
            
            // Calculate scaling factor. 409600 is a magic number.
            double scaleFactor = Math.Sqrt(Camera.main.pixelHeight * Camera.main.pixelWidth / (409600 * ratio));
            
            // Automatically set zoom level if it's less than zero.
            if (zoomLevel < 1)
            {
                zoomLevel = Mathf.RoundToInt((float) scaleFactor);
                zoomString = "auto";
            }
            
            // Calculate orthographic size with full precision and then convert to float precision.
            Camera.main.orthographicSize = Convert.ToSingle(ratio * 10 * scaleFactor / zoomLevel);
            
            // Recenter camera.
            DisplayManager.Instance.SetCameraFollowPos();
            
            // Set zoom string.
            zoomText.text = "Zoom: " + zoomString;
        }

        public void SetZoomLevel(float zoomLevel)
        {
            // Set the zoom string early, change if need be.
            zoomString = zoomLevel.ToString();
            this.zoomLevel = (int)zoomLevel;
            Refresh();
			PlayerPrefs.SetInt("CamZoom", this.zoomLevel);
			PlayerPrefs.Save();
        }

    }
}