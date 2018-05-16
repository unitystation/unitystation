using System;
using UnityEngine;

namespace UI
{	
    public class CameraZoomHandler : MonoBehaviour
    {
        private int zoomLevel = 1;

        void Start()
        {
            // Setup initial zoom level by guessing.
            SetZoomLevel(-1);
        }

        // Refreshes after setting zoom level.
        public void Refresh()
        {
            // Calculate ratio.
            double ratio = Screen.height / (double) Screen.width;
            // Calculate scaling factor. 409600 is a magic number.
            double scaleFactor = Math.Sqrt(Screen.height * Screen.width / (409600 * ratio));
            // Calculate orthographic size with full precision and then convert to float precision.
            Camera.main.orthographicSize = Convert.ToSingle(ratio * 10 * scaleFactor / zoomLevel);
            // Recenter camera.
            DisplayManager.Instance.SetCameraFollowPos();
        }

        public void SetZoomLevel(float zoomLevel)
        {
            int integerZoomLevel = (int)zoomLevel;
		
            // Automatically try to determine zoom level if we're feeding it some dumb values.
            if (integerZoomLevel < 1 || integerZoomLevel > 16)
            {			
                double ratio = Screen.height / (double) Screen.width;
                double scaleFactor = Math.Sqrt(Screen.height * Screen.width / (409600 * ratio));
                integerZoomLevel = Mathf.RoundToInt((float)scaleFactor);
            }

            this.zoomLevel = integerZoomLevel;
            Refresh();
        }

    }
}