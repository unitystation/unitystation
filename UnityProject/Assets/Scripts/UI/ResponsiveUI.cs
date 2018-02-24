using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UI
{
	/// <summary>
	///     Responds to any UI or game window size changes
	///     and adjusts all the elements accordingly
	///     (i.e. forcing 16:9 aspect ratio, resizing camera size etc)
	/// </summary>
	public class ResponsiveUI : MonoBehaviour
	{
		private readonly float targetAspect = 1.777f; // 16 : 9 aspect
		private CameraResizer camResizer;
		private CanvasScaler canvasScaler;
		private GraphicRaycaster graphicRaycaster;
		public RectTransform hudBottom;
		private bool isFullScreen;

		private bool monitorWindow;
		private Canvas parentCanvas;
		public RightPanelResize rightPanelResize;
		private bool checkingDisplayOnLoad = false;

		//Caches
		public float screenWidthCache { get; set; }

		public float screenHeightCache { get; set; }
		public float cacheWidth { get; set; }

		private void Start()
		{
			cacheWidth = rightPanelResize.panelRectTransform.sizeDelta.x;
			camResizer = FindObjectOfType<CameraResizer>();
			parentCanvas = GetComponent<Canvas>();
			canvasScaler = GetComponent<CanvasScaler>();
			graphicRaycaster = GetComponent<GraphicRaycaster>();
			if (!checkingDisplayOnLoad) {
				StartCoroutine(WaitForDisplay());
			}
		}

		private void OnEnable()
		{
			SceneManager.activeSceneChanged += OnSceneChange;
		}

		private void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		private void OnSceneChange(Scene last, Scene newScene){
			//Doesn't matter what InGame scene, just check the display on change:
			if (!checkingDisplayOnLoad && newScene.name != "Lobby") {
				StartCoroutine(WaitForDisplay());
			}
		}

		private IEnumerator WaitForDisplay()
		{
			checkingDisplayOnLoad = true;
			yield return new WaitForSeconds(0.2f);
			screenWidthCache = Screen.width;
			screenHeightCache = Screen.height;
			AdjustHudBottom(rightPanelResize.panelRectTransform.sizeDelta);
			monitorWindow = true;
			StartCoroutine(ForceGameWindowAspect());
		}

		private void Update()
		{
			//Check if window has changed and adjust the bottom hud
			if (monitorWindow)
			{
				if (screenWidthCache != Screen.width ||
				    screenHeightCache != Screen.height)
				{
					Invoke("AdjustHudBottomDelay", 0.1f);
					monitorWindow = false;
				}

				if (isFullScreen != Screen.fullScreen)
				{
					isFullScreen = Screen.fullScreen;
					Invoke("AdjustHudBottomDelay", 0.1f);
				}
			}
		}

		//It takes some time for the screen to redraw, wait for 0.1f
		private void AdjustHudBottomDelay()
		{
			AdjustHudBottom(rightPanelResize.panelRectTransform.sizeDelta);
			if (!Screen.fullScreen)
			{
				StopCoroutine(ForceGameWindowAspect());
				StartCoroutine(ForceGameWindowAspect());
			}
			else
			{
				monitorWindow = true;
			}
		}

		private IEnumerator ForceGameWindowAspect()
		{
			yield return new WaitForSeconds(0.2f);
			if (!Screen.fullScreen)
			{
				float screenWidth = Screen.height * targetAspect;

				//The following conditions check if the screen width or height
				//is an odd number. If it is, then it adjusted to be an even number
				//This fixes the sprite bleeding between tiles:
				if ((int) screenWidth % 2 != 0)
				{
					screenWidth += 1f;
				}
				int screenHeight = Screen.height;
				if (screenHeight % 2 != 0)
				{
					screenHeight++;
				}

				Screen.SetResolution((int) screenWidth, screenHeight, false);
				if (camResizer != null) {
					camResizer.AdjustCam();
				}
				Camera.main.ResetAspect();
				screenWidthCache = Screen.width;
				screenHeightCache = Screen.height;
			}
			//Refresh UI (helps avoid event system problems)
			parentCanvas.enabled = false;
			canvasScaler.enabled = false;
			graphicRaycaster.enabled = false;
			yield return new WaitForEndOfFrame();
			parentCanvas.enabled = true;
			canvasScaler.enabled = true;
			graphicRaycaster.enabled = true;
			monitorWindow = true;
			checkingDisplayOnLoad = false;
		}

		public void AdjustHudBottom(Vector2 panelRightSizeDelta)
		{
			Vector2 hudBottomSizeDelta = hudBottom.sizeDelta;
			//This is when pulling the rightpanel to the right of the screen from default position
			if (rightPanelResize.panelRectTransform.sizeDelta.x < cacheWidth)
			{
				//Calculate the new anchor point for hudBottom in the right direction of scale
				float panelRightProgress = (cacheWidth - rightPanelResize.panelRight.rect.width) / cacheWidth;
				float newAnchorPos = Mathf.Lerp(rightPanelResize.cacheHudAnchor, 0f, panelRightProgress);
				Vector2 anchoredPos = hudBottom.anchoredPosition;
				anchoredPos.x = -newAnchorPos;
				hudBottom.anchoredPosition = anchoredPos;
			}
			else
			{
				// this is for the left direction from the default position
				float panelRightProgress = (cacheWidth - rightPanelResize.panelRight.rect.width) / cacheWidth;
				float newAnchorPos = Mathf.Lerp(rightPanelResize.cacheHudAnchor, 562f, Mathf.Abs(panelRightProgress));
				Vector2 anchoredPos = hudBottom.anchoredPosition;
				anchoredPos.x = -newAnchorPos;
				hudBottom.anchoredPosition = anchoredPos;
				hudBottomSizeDelta.x = -panelRightSizeDelta.x;
				hudBottom.sizeDelta = hudBottomSizeDelta;
			}
			//KEEP ASPECT RATIO:
			hudBottomSizeDelta.y = hudBottom.rect.width * rightPanelResize.originalHudSize.y /
			                       rightPanelResize.originalHudSize.x;
			hudBottom.sizeDelta = hudBottomSizeDelta;
			UIManager.DisplayManager.SetCameraFollowPos(rightPanelResize.returnPanelButton.activeSelf);
			UIManager.PlayerHealthUI.overlayCrits.AdjustOverlayPos();
		}
	}
}