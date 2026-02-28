using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using US13.Core.Camera;
using US13.Core.Input_System;
using US13.Managers.SettingsManager;
using US13.Managers.UpdateManager;
using Util;

namespace US13.UI.Core
{
	public class CameraZoomHandler : MonoBehaviour
	{
		public float ZoomLevel => DisplaySettings.Instance.ZoomLevel;

		public bool ScrollWheelZoom => DisplaySettings.Instance.ScrollWheelZoom;


		private PixelPerfectCamera pixelPerfectCamera;

		void OnEnable()
		{
			DisplaySettings.Instance.SettingsChanged += DisplaySettings_SettingsChanged;
			SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
			UpdatePixelPerfectCamera();
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			DisplaySettings.Instance.SettingsChanged -= DisplaySettings_SettingsChanged;
			SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
		{
			UpdatePixelPerfectCamera();
		}

		private void UpdatePixelPerfectCamera()
		{
			// Connect up to the PixelPerfectCamera in the OnlineScene
			PixelPerfectCamera current = Camera.main.OrNull()?.GetComponent<PixelPerfectCamera>();

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


		/// <summary>
		/// Increment at which zoom changes when using Increase / DecreaseZoomLevel().
		/// </summary>
		private readonly int zoomIncrement = 32;

		void UpdateMe()
		{
			//Process any scroll wheel zooming:
			if (DisplaySettings.Instance.ScrollWheelZoom && !EventSystem.current.IsPointerOverGameObject())
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
			var view = Camera.main.ScreenToViewportPoint(CommonInput.mousePosition);
			return view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;
		}

		// Refreshes after setting zoom level.
		public void Refresh()
		{
			if (pixelPerfectCamera == null)
			{
				return; //probably in the lobby
			}

			pixelPerfectCamera.assetsPPU = DisplaySettings.Instance.ZoomLevel;

			if (Camera2DFollow.followControl != null)
			{
				Camera2DFollow.followControl.SetCameraXOffset();
			}

			StartCoroutine(RefreshUI());
		}


		private IEnumerator RefreshUI()
		{
			yield return WaitFor.EndOfFrame;
			Camera.main.GetComponent<CameraReferences>().UICamera.orthographicSize = Camera.main.orthographicSize;
		}


		public void SetZoomLevel(int _zoomLevel)
		{
			DisplaySettings.Instance.ZoomLevel = _zoomLevel;
		}

		/// <summary>
		/// A convenient way to increase zoom level
		/// <summary>
		public void IncreaseZoomLevel()
		{
			DisplaySettings.Instance.ZoomLevel += zoomIncrement;
		}

		/// <summary>
		/// A convenient way to increase zoom level
		/// ZoomLevel of 0 = Auto Zoom
		/// <summary>
		public void DecreaseZoomLevel()
		{
			DisplaySettings.Instance.ZoomLevel -= zoomIncrement;
		}

		public void SetScrollWheelZoom(bool activeState)
		{
			DisplaySettings.Instance.ScrollWheelZoom = activeState;
		}
	}
}