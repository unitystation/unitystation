using UnityEngine;

public class FulbrightRenderer : MonoBehaviour
{
	public Camera FulbrightCamera;

	private RenderTexture m_FulbrightRT;
	public RenderTexture _FulbrightRT
	{
		get
		{
			return m_FulbrightRT;
		}

		set
		{
			// Release old texture.
			if (m_FulbrightRT != null)
			{
				m_FulbrightRT.Release();
			}

			m_FulbrightRT = value;
		}
	}


	public void ResetRenderingTextures(OperationParameters iParameters)
	{
		// Prepare and assign RenderTexture.
		int _textureWidth = iParameters.screenSize.x;
		int _textureHeight = iParameters.screenSize.y;


		var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Fulbright RT";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;
		_newRenderTexture.filterMode = FilterMode.Point;

		_FulbrightRT = _newRenderTexture;


		FulbrightCamera.rect = iParameters.Rect;
		FulbrightCamera.orthographicSize = iParameters.cameraOrthographicSize;
	}

	public void Render(Camera iCameraToMatch)
	{
		FulbrightCamera.rect = iCameraToMatch.rect;
		FulbrightCamera.enabled = false;
		FulbrightCamera.backgroundColor = new Color(0, 0, 0, 0);
		FulbrightCamera.aspect             = iCameraToMatch.aspect;


		FulbrightCamera.targetTexture = _FulbrightRT;
		FulbrightCamera.Render();
	}

	public void SetUp(GameObject iRoot)
	{
		// Make sure camera is placed properly.
		FulbrightCamera.transform.localPosition = Vector3.zero;
		FulbrightCamera.transform.localScale = Vector3.one;
		FulbrightCamera.transform.localEulerAngles = Vector3.zero;

		FulbrightCamera.orthographic = true;
		FulbrightCamera.clearFlags = CameraClearFlags.Color;
		FulbrightCamera.backgroundColor = Color.clear;
		FulbrightCamera.depth = 9;
		FulbrightCamera.allowHDR = false;

		FulbrightCamera.nearClipPlane = -3f;
		FulbrightCamera.farClipPlane = 3f;
	}


}
