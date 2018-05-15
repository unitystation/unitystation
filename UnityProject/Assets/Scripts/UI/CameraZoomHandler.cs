using System;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class CameraZoomHandler : MonoBehaviour
{
	private int zoomLevel = 1;

	void Start()
	{
		// Setup initial zoom level by guessing.
		SetZoomLevel(-1);
	}
	
	// Update is called once per frame
	void Update()
	{
		// Calculate ratio.
		double ratio = Screen.height / (double) Screen.width;
		// Calculate scaling factor. 409600 is a magic number.
		double scaleFactor = Math.Sqrt(Screen.height * Screen.width / (409600 * ratio));
		// Calculate orthographic size with full precision and then convert to float precision.
		Camera.main.orthographicSize = Convert.ToSingle(ratio * 10 * scaleFactor / zoomLevel);
	}

	public void SetZoomLevel(int zoomLevel)
	{
		zoomLevel = (int)Mathf.Pow(2f,zoomLevel);
		// Automatically try to determine zoom level if we're feeding it some dumb values.
		if (zoomLevel < 1 || zoomLevel > 16)
		{			
			double ratio = Screen.height / (double) Screen.width;
			double scaleFactor = Math.Sqrt(Screen.height * Screen.width / (409600 * ratio));
			zoomLevel = Mathf.RoundToInt((float)scaleFactor);
		}

		this.zoomLevel = zoomLevel;
	}

}
