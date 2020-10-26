using AddressableReferences;
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

		[SerializeField]
		private AddressableAudioSource clickSound = null;

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

        void OnSceneLoaded(Scene oldScene, Scene newScene)
        {
            DetermineActiveState(newScene);
        }

        /// <summary>
        /// Should buttons be showing or not
        /// </summary>
        void DetermineActiveState(Scene scene)
        {
            if (scene.name.Contains("Lobby"))
            {
                panel.SetActive(false);
            }
            else
            {
                panel.SetActive(true);
            }
        }

        public void OnZoomIn()
        {
			SoundManager.Play(clickSound, string.Empty);
			CamZoomHandler.IncreaseZoomLevel();

        }

        public void OnZoomOut()
        {
			SoundManager.Play(clickSound, string.Empty);
			CamZoomHandler.DecreaseZoomLevel();
        }

        public void OpenOptionsMenu()
        {
			SoundManager.Play(clickSound, string.Empty);
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