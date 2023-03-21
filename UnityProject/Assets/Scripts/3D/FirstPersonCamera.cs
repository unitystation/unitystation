using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FirstPersonCamera : MonoBehaviour
{
	public float mouseSensitivity = 100f;


	private float xRotation = 0f;
	private float yRotation = 0f;

	void Update()
	{
		if (Input.GetKey(KeyCode.E))
		{
			float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
			float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

			xRotation -= mouseY;
			xRotation = Mathf.Clamp(xRotation, -90f, 90f);

			yRotation -= mouseX;
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
		}

		if (Input.GetKey(KeyCode.F8))
		{
			var Sprites = FindObjectsOfType<SpriteRenderer>();
			foreach (var Sprite in Sprites)
			{
				if (Sprite.name.Contains("Square")) continue;
				Sprite.sortingOrder = 0;
				Sprite.sortingLayerName = "Default";
			}

			var Orders = FindObjectsOfType<SortingGroup>();
			foreach (var Order in Orders)
			{
				Order.sortingOrder = 1;
				Order.sortingLayerName = "Walls";
			}
		}
	}
}