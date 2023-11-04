using UnityEngine;
using UnityEngine.Rendering;

namespace _3D
{
	public class FirstPersonCamera : MonoBehaviour
	{
		public float mouseSensitivity = 100f;


		private float xRotation = 0f;
		private float yRotation = 0f;


		private LightingSystem LightingSystem;

		public void Awake()
		{
			LightingSystem = Camera.main.GetComponent<LightingSystem>();
			RandomiseSkybox();
		}

		void Update()
		{
			LightingSystem.enabled = false;

			if (Input.GetKey(KeyCode.Tab) == false && Application.isFocused && UIManager.Instance.isInputFocus == false)
			{

				float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
				float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

				xRotation -= mouseY;
				xRotation = Mathf.Clamp(xRotation, -90f, 90f);

				yRotation += mouseX;
				if (yRotation > 180)
				{
					yRotation = -180;
				}
				else if (yRotation < -180)
				{
					yRotation = 180;
				}

				transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
				//playerBody.Rotate(Vector3.up * mouseX);

				// Set the mouse position to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}


			RandomiseSkyboxOnDemand();
		}


		private void RandomiseSkybox()
		{
			GetComponent<Skybox>().material = SkyboxData.Instance.SkyboxMaterials.PickRandom();
			Camera.main.clearFlags = CameraClearFlags.Skybox;
		}

		private void RandomiseSkyboxOnDemand()
		{
			if (Input.GetKeyDown(KeyCode.PageDown)) RandomiseSkybox();
		}
	}
}