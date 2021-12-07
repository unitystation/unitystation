using UnityEngine;

public class UseMainCameraSize : MonoBehaviour
{
	private Camera Camera;
	private Camera MainCamera;

	// Use this for initialization
	private void Start()
	{
		Camera = GetComponent<Camera>();
		MainCamera = Camera.main;
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