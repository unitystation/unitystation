using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;

public class CameraZoomHandler : MonoBehaviour
{
	public float ZoomLevel => displaySettings.ZoomLevel;

	public bool ScrollWheelZoom => displaySettings.ScrollWheelZoom;

	private DisplaySettings displaySettings = null;
	private PixelPerfectCamera pixelPerfectCamera;

	void OnEnable()
	{
		displaySettings.SettingsChanged += DisplaySettings_SettingsChanged;
		SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
		UpdatePixelPerfectCamera();
	}

	private void OnDisable()
	{
		displaySettings.SettingsChanged -= DisplaySettings_SettingsChanged;
		SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
	}

	private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
	{
		UpdatePixelPerfectCamera();
	}

	private void UpdatePixelPerfectCamera()
	{
		// Connect up to the PixelPerfectCamera in the OnlineScene
		PixelPerfectCamera current = Camera.main.GetComponent<PixelPerfectCamera>();

		// Discard our old reference if it is from an old OnlineScene
		if (current != null && pixelPerfectCamera != current)
		{
			pixelPerfectCamera = current;
		}

		Refresh();
	}

	private void DisplaySettings_SettingsChanged(object sender, DisplaySettings.DisplaySettingsChangedEventArgs e)
	{
		if (e.ZoomLevelChanged)
		{
			Refresh();
		}
	}

	void Awake()
	{
		displaySettings = FindObjectOfType<DisplaySettings>();
	}

	/// <summary>
	/// Increment at which zoom changes when using Increase / DecreaseZoomLevel().
	/// </summary>
	private readonly int zoomIncrement = 8;

	void Update()
	{
		//Process any scroll wheel zooming:
		if (displaySettings.ScrollWheelZoom && !EventSystem.current.IsPointerOverGameObject())
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
		if (pixelPerfectCamera == null)
		{
			return; //probably in the lobby
		}

		pixelPerfectCamera.assetsPPU = displaySettings.ZoomLevel;

		if (Camera2DFollow.followControl != null)
		{
			Camera2DFollow.followControl.SetCameraXOffset();
		}
	}

	public void SetZoomLevel(int _zoomLevel)
	{
		displaySettings.ZoomLevel = _zoomLevel;
	}

	/// <summary>
	/// A convenient way to increase zoom level
	/// <summary>
	public void IncreaseZoomLevel()
	{
		displaySettings.ZoomLevel += zoomIncrement;
	}

	/// <summary>
	/// A convenient way to increase zoom level
	/// ZoomLevel of 0 = Auto Zoom
	/// <summary>
	public void DecreaseZoomLevel()
	{
		displaySettings.ZoomLevel -= zoomIncrement;
	}

	public void SetScrollWheelZoom(bool activeState)
	{
		displaySettings.ScrollWheelZoom = activeState;
	}
}