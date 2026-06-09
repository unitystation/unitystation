using System;
using UnityEngine;
using UnityEngine.U2D;

public class ShadowMaskRenderer : MonoBehaviour
{
	public Camera mTableMaskCamera;
	public Camera mWallMaskCamera; //It is because the other one is pixel perfect for some reason
	public Camera mShadowCamera;
	private Vector3 mPreviousCameraPosition;
	private Vector2 mPreviousFilteredPosition;

	private RenderTexture m_pingRT;
	private RenderTexture m_pongRT;
	private RenderTexture m_tableRT;
	private RenderTexture m_WallRT;



	public RenderTexture _WallRT
	{
		get
		{
			return m_WallRT;
		}

		set
		{
			// Release old texture.
			if (m_WallRT != null)
			{
				m_WallRT.Release();
			}

			m_WallRT = value;
		}
	}


	public RenderTexture _pingRT
	{
		get
		{
			return m_pingRT;
		}

		set
		{
			// Release old texture.
			if (m_pingRT != null)
			{
				m_pingRT.Release();
			}

			m_pingRT = value;
		}
	}



	private RenderTexture _pongRT
	{
		get
		{
			return m_pongRT;
		}

		set
		{
			// Release old texture.
			if (m_pongRT != null)
			{
				m_pongRT.Release();
			}

			m_pongRT = value;
		}
	}

	private RenderTexture _tableRT
	{
		get
		{
			return m_tableRT;
		}

		set
		{
			// Release old texture.
			if (m_tableRT != null)
			{
				m_tableRT.Release();
			}

			m_tableRT = value;
		}
	}



	[Header("Shadow — Shaders")]
	[Tooltip("ShadowSilhouette.shader — renders casters as flat black into the shadow RT.")]
	public Shader   silhouetteShader;
	[Tooltip("Material using ShadowBlur.shader — separable Gaussian blur + classification pass.")]
	public Material blurMaterial;

	public int   blurPasses = 2;

	public static float shadowAlpha = 0.4f;

	public static float blurSpread = 10f;

	public void SetUp(GameObject iRoot)
	{
		// Setup camera based on main camera.
		SetUpCameraObject(mTableMaskCamera);
		SetUpCameraObject(mShadowCamera);
	}

	public void ResetRenderingTextures(OperationParameters iParameters)
	{
		// Prepare and assign RenderTexture.
		int _textureWidth = iParameters.screenSize.x;
		int _textureHeight = iParameters.screenSize.y;

		var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Table Mask";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		_tableRT = _newRenderTexture;


		_newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "pong";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		_pongRT = _newRenderTexture;


		_newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "ping";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		_pingRT = _newRenderTexture;


		_newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "_WallRT";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		_WallRT = _newRenderTexture;



		mTableMaskCamera.orthographicSize = iParameters.cameraOrthographicSize; // * 1.003f;
		mShadowCamera.orthographicSize = iParameters.cameraOrthographicSize; //* 1.003f;
		mWallMaskCamera.orthographicSize = iParameters.cameraOrthographicSize;
	}

	public void Render(Camera iCameraToMatch)
	{

		mTableMaskCamera.enabled = false;
		mTableMaskCamera.backgroundColor = new Color(0, 0, 0, 0);
		mTableMaskCamera.aspect             = iCameraToMatch.aspect;

		mShadowCamera.enabled = false;
		mShadowCamera.backgroundColor = new Color(0, 0, 0, 0); ;
		mShadowCamera.aspect             = iCameraToMatch.aspect;

		mWallMaskCamera.enabled = false;
		mWallMaskCamera.backgroundColor = new Color(0, 0, 0, 0); ;
		mWallMaskCamera.aspect             = iCameraToMatch.aspect;


		mShadowCamera.targetTexture = _pingRT;
		mShadowCamera.RenderWithShader(silhouetteShader, "RenderType");

		mTableMaskCamera.targetTexture = _tableRT;
		mTableMaskCamera.RenderWithShader(silhouetteShader, "RenderType");


		mWallMaskCamera.targetTexture = _WallRT;
		mWallMaskCamera.RenderWithShader(silhouetteShader, "RenderType");

		blurMaterial.SetTexture("_WallFloorOcclusionTex", _WallRT);
		blurMaterial.SetTexture("_TableOcclusionTex",     _tableRT);

		Graphics.Blit(_pingRT, _pongRT, blurMaterial, 1);

		Graphics.Blit(_pongRT, _pingRT);

		for (int i = 0; i < blurPasses; i++)
		{
			blurMaterial.SetVector("_Direction", new Vector4(1, 0, 0, 0));
			blurMaterial.SetFloat ("_Spread",    blurSpread /iCameraToMatch.orthographicSize );
			Graphics.Blit(_pingRT, _pongRT, blurMaterial, 0);

			blurMaterial.SetVector("_Direction", new Vector4(0, 1, 0, 0));
			Graphics.Blit(_pongRT, _pingRT, blurMaterial, 0);
		}


	}

	private void SetUpCameraObject(Camera iSetupCamera)
	{
		// Make sure camera is placed properly.
		iSetupCamera.transform.localPosition = Vector3.zero;
		iSetupCamera.transform.localScale = Vector3.one;
		iSetupCamera.transform.localEulerAngles = Vector3.zero;

		iSetupCamera.orthographic = true;
		iSetupCamera.clearFlags = CameraClearFlags.Color;
		iSetupCamera.backgroundColor = Color.black;
		iSetupCamera.depth = 9;
		iSetupCamera.allowHDR = false;

		iSetupCamera.nearClipPlane = -3f;
		iSetupCamera.farClipPlane = 3f;
	}




}
