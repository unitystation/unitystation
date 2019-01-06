using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


	/// <summary>
	///     Responds to any UI or game window size changes
	///     and adjusts all the elements accordingly
	///     (i.e. forcing 16:9 aspect ratio, resizing camera size etc)
	/// </summary>
	public class ResponsiveUI : MonoBehaviour
	{
//		private readonly float targetAspect = 1.777f; // 16 : 9 aspect
		private CameraResizer camResizer;
		private CanvasScaler canvasScaler;
		private GraphicRaycaster graphicRaycaster;
		private CameraZoomHandler cameraZoomHandler;

		private bool monitorWindow;
		private Canvas parentCanvas;
		private bool checkingDisplayOnLoad = false;

		//Caches
		public float screenWidthCache { get; set; }

		public float screenHeightCache { get; set; }
		public float cacheWidth { get; set; }

		private void Start()
		{
			//cacheWidth = rightPanelResize.panelRectTransform.sizeDelta.x;
			camResizer = FindObjectOfType<CameraResizer>();
			parentCanvas = GetComponent<Canvas>();
			canvasScaler = GetComponent<CanvasScaler>();
			cameraZoomHandler = GetComponent<CameraZoomHandler>();
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
			//AdjustHudBottom(rightPanelResize.panelRectTransform.sizeDelta);
			monitorWindow = true;
			if (!Screen.fullScreen) {
				StartCoroutine(ForceGameWindowAspect());
			}
#if UNITY_EDITOR
			StartCoroutine( ForceGameWindowAspect() );
#endif
		}

		private void Update()
		{
			//Check if window has changed and adjust the bottom hud
			if (monitorWindow)
			{
				if (screenWidthCache != Screen.width ||
				    screenHeightCache != Screen.height)
				{
					StartCoroutine(ForceGameWindowAspect());
					monitorWindow = false;
				}
			}

			if(KeyboardInputManager.IsEscapePressed()){
				Screen.fullScreen = false;
			}
		}

		private IEnumerator ForceGameWindowAspect()
		{
			yield return new WaitForSeconds(1.2f);
			//The following conditions check if the screen width or height
			//is an odd number. If it is, then it adjusted to be an even number
			//This fixes the sprite bleeding between tiles:
			int width = Screen.width;
			if (width % 2 != 0)
			{
//			Logger.Log( $"Odd width {width}->{width-1}" );
				width--;
			}
			int height = Screen.height;
			if (height % 2 != 0)
			{
//			Logger.Log( $"Odd height {height}->{height-1}" );
				height--;
			}

//			Logger.Log("Screen height before resizing: " + Camera.main.pixelHeight + " Aspect Y: " + height/(float)Screen.height);
//			Logger.Log("Screen height before resizing: " + Camera.main.pixelWidth + " Aspect X: " + width/(float)Screen.width);

			// Enforce aspect by resizing the camera rectangle to nearest (lower) even number.
			Camera.main.rect = new Rect(0, 0, width / (float)Screen.width, height / (float)Screen.height);

//		Logger.Log("Screen height after resizing: " + Camera.main.pixelHeight);

			if (camResizer != null) {
				camResizer.AdjustCam();
			}
			screenWidthCache = Screen.width;
			screenHeightCache = Screen.height;

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
			cameraZoomHandler.Refresh();
		}
	}
