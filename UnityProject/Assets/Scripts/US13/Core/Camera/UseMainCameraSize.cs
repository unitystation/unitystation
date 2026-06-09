using UnityEngine;
using US13.Managers.UpdateManager;

namespace US13.Core.Camera
{
	public class UseMainCameraSize : MonoBehaviour
	{
		private UnityEngine.Camera Camera;
		private UnityEngine.Camera MainCamera;

		// Use this for initialization
		private void Start()
		{
			Camera = GetComponent<UnityEngine.Camera>();
			MainCamera = UnityEngine.Camera.main;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		// Update is called once per frame
		private void UpdateMe()
		{
			if (MainCamera != null && Camera != null)
			{
				Camera.orthographicSize = MainCamera.orthographicSize;
			}
		}
	}
}