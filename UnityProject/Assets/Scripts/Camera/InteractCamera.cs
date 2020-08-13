using UnityEngine;

public class InteractCamera : MonoBehaviourSingleton<InteractCamera>
{
	public Camera interactCam;
	public Camera mainCam;

	private void Start()
	{
		interactCam.orthographicSize = mainCam.orthographicSize;
	}

	private void Update()
	{
		if (interactCam.orthographicSize != mainCam.orthographicSize)
		{
			interactCam.orthographicSize = mainCam.orthographicSize;
		}
	}
}