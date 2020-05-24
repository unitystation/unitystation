using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;

public class CameraZoomHandler : MonoBehaviour
{
	public float ZoomLevel => zoomLevel;
	private int zoomLevel = DEFAULT_ZOOMLEVEL;
	private const int DEFAULT_ZOOMLEVEL = 24;
	
	public bool ScrollWheelZoom => scrollWheelzoom;
	private bool scrollWheelzoom = DEFAULT_SCROLLWHEELZOOM;
	private const bool DEFAULT_SCROLLWHEELZOOM = true;

    private PixelPerfectCamera pixelPerfectCamera;

	void Awake()
	{
		pixelPerfectCamera = Camera.main.GetComponent<PixelPerfectCamera>();

		if (PlayerPrefs.HasKey(PlayerPrefKeys.CamZoomKey))
		{
			zoomLevel = PlayerPrefs.GetInt(PlayerPrefKeys.CamZoomKey);
		}
		else
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.CamZoomKey, DEFAULT_ZOOMLEVEL);
			PlayerPrefs.Save();
		}

		if (PlayerPrefs.HasKey(PlayerPrefKeys.ScrollWheelZoom))
		{
			scrollWheelzoom = PlayerPrefs.GetInt(PlayerPrefKeys.ScrollWheelZoom) == 1;
		}
		else
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, DEFAULT_SCROLLWHEELZOOM ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	void Start()
    {
        Refresh();
    }

	/// <summary>
	/// Maximum allowed zoom value.
	/// </summary>
	private static int maxZoom = 64;

	/// <summary>
	/// Minimum allowed zoom value.
	/// </summary>
	private readonly int minZoom = 8;

	/// <summary>
	/// Increment at which zoom changes when using Increase / DecreaseZoomLevel().
	/// </summary>
	private readonly int zoomIncrement = 8;

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
	    if(pixelPerfectCamera == null) pixelPerfectCamera = Camera.main.GetComponent<PixelPerfectCamera>();
	    if (pixelPerfectCamera == null) return; //probably in the lobby
	    zoomLevel = Mathf.Clamp(zoomLevel, minZoom, maxZoom);
        pixelPerfectCamera.assetsPPU = zoomLevel;

        if (Camera2DFollow.followControl != null)
        {
	        Camera2DFollow.followControl.SetCameraXOffset();
        }
    }

    public void SetZoomLevel(int _zoomLevel)
    {
	    zoomLevel = _zoomLevel;
	    Refresh();
	    PlayerPrefs.SetInt(PlayerPrefKeys.CamZoomKey, Mathf.Clamp(zoomLevel, minZoom, maxZoom));
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

    public void SetScrollWheelZoom(bool activeState)
    {
        scrollWheelzoom = activeState;
		PlayerPrefs.SetInt(PlayerPrefKeys.ScrollWheelZoom, activeState ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ResetDefaults()
    {
        SetScrollWheelZoom(DEFAULT_SCROLLWHEELZOOM);
        SetZoomLevel(DEFAULT_ZOOMLEVEL);
    }
}