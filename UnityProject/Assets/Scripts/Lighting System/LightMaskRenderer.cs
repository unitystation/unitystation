using System;
using System.Linq;
using UnityEngine;

public class LightMaskRenderer : MonoBehaviour
{
	private const string MaskCameraName = "Light Mask Camera";

	private Camera mMaskCamera;
	private RenderTexture mMask;
	
	private RenderTexture mask
	{
		get
		{
			return mMask;
		}

		set
		{
			// Release old texture.
			if (mMask != null)
			{
				mMask.Release();
			}

			// Assign new one. May be null.
			mMask = value;
			mMaskCamera.targetTexture = mMask;
		}
	}

	public static LightMaskRenderer InitializeMaskRenderer(
		GameObject iRoot)
	{
		// Get or create base camera.
		var _cameraTransform = iRoot.transform.Find(MaskCameraName);

		if (_cameraTransform == null)
			_cameraTransform = CreateNewCameraGo(iRoot, MaskCameraName);

		var _maskCamera = _cameraTransform.GetComponent<Camera>();

		if (_maskCamera == null)
			throw new Exception("LightMaskRenderer Unable to properly initialize mask camera.");

		// Setup camera based on main camera.
		var _maskProcessor = SetUpCameraObject(_maskCamera);

		return _maskProcessor;
	}

	public void ResetRenderingTextures(MaskParameters iParameters)
	{
		// Prepare and assign RenderTexture.
		int _textureWidth = iParameters.lightTextureSize.x;
		int _textureHeight = iParameters.lightTextureSize.y;

		var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Raw Light Mask";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;
		_newRenderTexture.filterMode = FilterMode.Bilinear;
		_newRenderTexture.antiAliasing = iParameters.antiAliasing;

		// Note: Assignment will release previous texture if exist.
		mask = _newRenderTexture;

		mMaskCamera.orthographicSize = iParameters.cameraOrthographicSize;

		Vector2 _scale = new Vector2((float)iParameters.cameraOrthographicSize / iParameters.extendedCameraSize, (float)iParameters.cameraOrthographicSize / iParameters.extendedCameraSize);
		Shader.SetGlobalVector("_ExtendedToSmallTextureScale", _scale);
	}

	public RenderTexture Render(RenderSettings iRenderSettings)
	{
		mMaskCamera.enabled = false;
		mMaskCamera.cullingMask = iRenderSettings.lightSourceLayers; 

		mMaskCamera.backgroundColor = Color.black;

		mMaskCamera.Render();

		return mask;
	}

	private static Transform CreateNewCameraGo(GameObject iRoot, string iMaskLayerName)
	{
		var _cameraGameObject = new GameObject(iMaskLayerName);
		_cameraGameObject.transform.parent = iRoot.transform;
		_cameraGameObject.AddComponent<Camera>();

		return _cameraGameObject.transform;
	}

	private static LightMaskRenderer SetUpCameraObject(
		Camera iSetupCamera)
	{
		// Make sure camera is placed properly.
		iSetupCamera.transform.localPosition = Vector3.zero;
		iSetupCamera.transform.localScale = Vector3.one;
		iSetupCamera.transform.localEulerAngles = Vector3.zero;

		// Note: Avoided CopyFrom since main camera setting can change and include something that will break mask camera.
		iSetupCamera.orthographic = true;
		iSetupCamera.clearFlags = CameraClearFlags.Color;
		iSetupCamera.depth = 10;
		iSetupCamera.allowHDR = false;
		iSetupCamera.allowMSAA = false;
		iSetupCamera.farClipPlane = 3f;

		// Get or add processor component.
		var _processor = iSetupCamera.gameObject.GetComponent<LightMaskRenderer>();

		if (_processor == null)
			_processor = iSetupCamera.gameObject.AddComponent<LightMaskRenderer>();

		return _processor;
	}

	private void Awake()
	{
		mMaskCamera = gameObject.GetComponent<Camera>();
	}
}