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

	// Update is called once per frame
	private void Update()
	{
		if (MainCamera != null && Camera != null)
		{
			Camera.orthographicSize = MainCamera.orthographicSize;
		}
	}
}