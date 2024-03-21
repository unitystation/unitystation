using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

public class ShuttleCameraRenderer : MonoBehaviour
{
	public Camera renderCamera; // Assign the camera to render
	public RenderTexture renderTexture; // Assign the Render Texture
	public Material materialRadarGreen;

	public static Texture2D Intermediatetexture;
	public static Texture2D texture;
	public static Sprite UISprite;

	public static ShuttleCameraRenderer instance;

	public LightingSystem LightingSystem;

	void Start()
	{

		instance = this;
		// Set the Render Texture to the camera
		renderCamera.targetTexture = renderTexture;
		// Force the camera to render into the Render Texture
		renderCamera.Render();

		// Make sure the RenderTexture is active
		RenderTexture.active = renderTexture;

		// Create a new Texture2D and read the pixels from the RenderTexture
		texture = new Texture2D(renderTexture.width, renderTexture.height);
		Intermediatetexture = new Texture2D(renderTexture.width, renderTexture.height);
		materialRadarGreen.SetTexture("_SecondTex", texture);
		// Create a new Texture2D and read the pixels from the RenderTexture
		texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		texture.Apply();

		// Set the texture directly on the Image component
		UISprite = Sprite.Create(
			texture: texture,
			rect: new Rect(0, 0, renderTexture.width, renderTexture.height),
			pivot: new Vector2(0.5f, 0.5f)
		);
		RenderTexture.active  = null;

	}

	public void Update()
	{
		if (CustomNetworkManager.IsHeadless) return;

		if (PlayerManager.LocalPlayerObject != null)
		{
			var MatrixMove = PlayerManager.LocalPlayerObject.GetComponentInParent<MatrixMove>();
			if (MatrixMove != null)
			{
				if (MatrixMove.NetworkedMatrixMove.TargetOrientation != OrientationEnum.Default
				    || MatrixMove.NetworkedMatrixMove.WorldCurrentVelocity.magnitude > 0
				    || MatrixMove.NetworkedMatrixMove.CurrentTorque > 0)
				{
					LightingSystem.matrixRotationMode = true;
				}
				else
				{
					LightingSystem.matrixRotationMode = false;
				}


			}
		}
	}

	public void UpdateME()
	{
		if (CustomNetworkManager.IsHeadless) return;

		if (PlayerManager.LocalPlayerObject != null)
		{
			var MatrixMove = PlayerManager.LocalPlayerObject.GetComponentInParent<MatrixMove>();
			if (MatrixMove != null)
			{
				float angleInRadians = Mathf.Atan2(MatrixMove.NetworkedMatrixMove.ForwardsDirection.y, MatrixMove.NetworkedMatrixMove.ForwardsDirection.x);
				float angleInDegrees = angleInRadians * Mathf.Rad2Deg; ;
				renderCamera.transform.localRotation = Quaternion.Euler(new Vector3(0,0, angleInDegrees-90));
			}

		}

		// Set the Render Texture to the camera
		renderCamera.targetTexture = renderTexture;
		// Force the camera to render into the Render Texture
		renderCamera.Render();

		// Make sure the RenderTexture is active
		RenderTexture.active = renderTexture;
		materialRadarGreen.SetTexture("_SecondTex", texture);
		// Create a new Texture2D and read the pixels from the RenderTexture
		Intermediatetexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		Intermediatetexture.Apply();

		Graphics.Blit(Intermediatetexture, renderTexture, materialRadarGreen);

		// Create a new Texture2D and read the pixels from the RenderTexture
		texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
		texture.Apply();

		RenderTexture.active = null;
	}
}
