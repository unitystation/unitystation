using UnityEngine;
using US13.Managers.UpdateManager;

namespace US13.Core.Camera
{
	public class InteractCamera : MonoBehaviour
	{
		public static InteractCamera Instance;
		public UnityEngine.Camera interactCam;
		public UnityEngine.Camera mainCam;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		private void Start()
		{
			interactCam.orthographicSize = mainCam.orthographicSize;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (interactCam.orthographicSize != mainCam.orthographicSize)
			{
				interactCam.orthographicSize = mainCam.orthographicSize;
			}
		}
	}
}