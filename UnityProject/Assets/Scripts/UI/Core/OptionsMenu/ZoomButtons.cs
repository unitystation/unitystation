using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unitystation.Options
{
    public class ZoomButtons : MonoBehaviour
    {
        [SerializeField]
        private GameObject panel = null;
        private CameraZoomHandler camZoomHandler;
        private CameraZoomHandler CamZoomHandler
        {
            get
            {
                if (camZoomHandler == null)
                {
                    camZoomHandler = FindObjectOfType<CameraZoomHandler>();
                }
                return camZoomHandler;
            }
        }

		#region Lifecycle

		void Start()
        {
            DetermineActiveState(SceneManager.GetActiveScene());
        }

        void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnSceneLoaded;
        }

		#endregion Lifecycle

		void OnSceneLoaded(Scene oldScene, Scene newScene)
        {
            DetermineActiveState(newScene);
        }

        /// <summary>
        /// Should buttons be showing or not
        /// </summary>
        void DetermineActiveState(Scene scene)
        {
			panel.SetActive(scene.name.Contains("Lobby") == false);
        }

        public void OnZoomIn()
        {
            _ = SoundManager.Play(CommonSounds.Instance.Click01);
            CamZoomHandler.IncreaseZoomLevel();

        }

        public void OnZoomOut()
        {
            _ = SoundManager.Play(CommonSounds.Instance.Click01);
            CamZoomHandler.DecreaseZoomLevel();
        }

        public void OpenOptionsMenu()
        {
            _ = SoundManager.Play(CommonSounds.Instance.Click01);
            OptionsMenu.Instance.Open();
        }

        public void OpenPlayerList()
        {
	        if (!UIManager.Instance.lobbyUIPlayerListController.gameObject.activeSelf)
	        {
		        UIManager.Instance.lobbyUIPlayerListController.GenerateList();
		        UIManager.Instance.lobbyUIPlayerListController.gameObject.SetActive(true);
	        }
	        else
	        {
		        UIManager.Instance.lobbyUIPlayerListController.gameObject.SetActive(false);
	        }
        }
    }
}
