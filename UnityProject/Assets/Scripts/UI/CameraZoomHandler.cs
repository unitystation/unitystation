using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraZoomHandler : MonoBehaviour
{
    private float zoomLevel = 2f;
    public float ZoomLevel => zoomLevel;
    private bool scrollWheelzoom = false;
    public bool ScrollWheelZoom => scrollWheelzoom;

    void Start()
    {
        if (PlayerPrefs.HasKey(PlayerPrefKeys.CamZoomKey))
        {
            zoomLevel = PlayerPrefs.GetFloat(PlayerPrefKeys.CamZoomKey);
            scrollWheelzoom = PlayerPrefs.GetInt(PlayerPrefKeys.ScrollWheelZoom) == 1;
        }
        else
        {
            zoomLevel = 2f;
            PlayerPrefs.SetFloat(PlayerPrefKeys.CamZoomKey, 2f);
            PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, 1);
            PlayerPrefs.Save();
        }

        Refresh();
    }

	/// <summary>
	/// Maximum allowed zoom value.
	/// </summary>
	private static float maxZoom = 4f;

	/// <summary>
	/// Minimum allowed zoom value.
	/// </summary>
	private readonly float minZoom = 0.6f;

	/// <summary>
	/// Increment at which zoom changes when using Increase / DecreaseZoomLevel().
	/// </summary>
	private readonly float zoomIncrement = 0.2f;

    void Update()
    {
        //Process any scroll wheel zooming:
        if (scrollWheelzoom && !EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.mouseScrollDelta.y > 0f)
            {
                if (!MouseOutside()) IncreaseZoomLevel();
            }

            if (Input.mouseScrollDelta.y < 0f)
            {
                if (!MouseOutside()) DecreaseZoomLevel();
            }
        }
    }

    bool MouseOutside()
    {
        var view = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        return view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;
    }

    // Refreshes after setting zoom level.
    public void Refresh()
    {
        var cam = Camera.main;

        zoomLevel = Mathf.Clamp(zoomLevel, minZoom, maxZoom);
        // Calculate orthographic size with full precision and then convert to float precision.
        cam.orthographicSize = ((float)cam.pixelWidth / ((float)zoomLevel * 64f)) * 0.5f;

        // Recenter camera.
        DisplayManager.Instance.SetCameraFollowPos();
    }

    public void SetZoomLevel(float _zoomLevel)
    {
	    zoomLevel = _zoomLevel;
	    Refresh();
	    PlayerPrefs.SetFloat(PlayerPrefKeys.CamZoomKey, Mathf.Clamp(zoomLevel, minZoom, maxZoom));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// A convenient way to increase zoom level
    /// <summary>
    public void IncreaseZoomLevel()
    {
		zoomLevel += zoomIncrement;
        SetZoomLevel(zoomLevel);
    }

    /// <summary>
    /// A convenient way to increase zoom level
    /// ZoomLevel of 0 = Auto Zoom
    /// <summary>
    public void DecreaseZoomLevel()
    {
	    zoomLevel -= zoomIncrement;
        SetZoomLevel(zoomLevel);
    }

    public void ToggleScrollWheelZoom(bool activeState)
    {
        scrollWheelzoom = activeState;
        if (activeState)
        {
            PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, 1);
        }
        else
        {
            PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, 0);
        }
        PlayerPrefs.Save();
    }

    public void ResetDefaults()
    {
        ToggleScrollWheelZoom(false);
        SetZoomLevel(2f);
    }
}