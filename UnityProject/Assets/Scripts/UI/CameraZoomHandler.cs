﻿using System;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{	
    public class CameraZoomHandler : MonoBehaviour
    {
        //Reference to zoom text.
        public Text zoomText;
        private int zoomLevel = 2;
        private string zoomString = "2";
        public Slider zoomSlider;

		void Start(){
			if (PlayerPrefs.HasKey("CamZoomSetting")){
				zoomLevel = PlayerPrefs.GetInt("CamZoomSetting");
                zoomSlider.value = zoomLevel;
			} else {
                zoomLevel = 2;
                zoomSlider.value = 2;
				PlayerPrefs.SetInt("CamZoomSetting", 2);
				PlayerPrefs.Save();
			}

            zoomString = zoomLevel.ToString();
            Refresh();
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
			PlayerPrefs.SetInt("CamZoomSetting", this.zoomLevel);
			PlayerPrefs.Save();
        }

    }
}