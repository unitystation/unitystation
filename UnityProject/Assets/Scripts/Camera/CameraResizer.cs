using UnityEngine;

public class CameraResizer : MonoBehaviour
{
	public float fWidth = 9.0f; // Desired width 

	private void Start()
	{
		AdjustCam();
	}

	//Adjusts cam in relation to game window size
	public void AdjustCam()
	{
		float fT = fWidth / Screen.width * Screen.height;
		fT = fT / (2.0f * Mathf.Tan(0.5f * Camera.main.fieldOfView * Mathf.Deg2Rad));
		Vector3 v3T = Camera.main.transform.position;
		v3T.z = -fT;
		transform.position = v3T;
	}
}