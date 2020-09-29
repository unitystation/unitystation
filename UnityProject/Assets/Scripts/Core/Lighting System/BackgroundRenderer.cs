using System;
using System.Linq;
using UnityEngine;

public class BackgroundRenderer : MonoBehaviour
{
	private const string MaskCameraName = "Background Mask Camera";

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

	public static BackgroundRenderer InitializeMaskRenderer(GameObject iRoot)
	{
		// Get or create base camera.
		var _cameraTransform = iRoot.transform.Find(MaskCameraName);

		if (_cameraTransform == null)
			_cameraTransform = CreateNewCameraGo(iRoot, MaskCameraName);

		var _maskCamera = _cameraTransform.GetComponent<Camera>();

		if (_maskCamera == null)
			throw new Exception("BackgroundRenderer Unable to properly initialize mask camera.");

		// Setup camera based on main camera.
		var _maskProcessor = SetUpCameraObject(_maskCamera);

		return _maskProcessor;
	}

	public RenderTexture Render(RenderSettings iRenderSettings)
	{
		// Setup.
		mMaskCamera.enabled = false;
		mMaskCamera.cullingMask = iRenderSettings.backgroundLayers; 
		mMaskCamera.backgroundColor = Color.black;

		mMaskCamera.Render();

		return mask;
	}

	public void ResetRenderingTextures(OperationParameters iParameters)
	{
		// Prepare and assign RenderTexture.
		int _textureWidth = iParameters.screenSize.x;
		int _textureHeight = iParameters.screenSize.y;

		var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Background Mask";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		// Note: Assignment will release previous texture if exist.
		mask = _newRenderTexture;

		mMaskCamera.orthographicSize = iParameters.cameraOrthographicSize;
	}

	private static Transform CreateNewCameraGo(GameObject iRoot, string iMaskLayerName)
	{
		var _cameraGameObject = new GameObject(iMaskLayerName);
		_cameraGameObject.transform.parent = iRoot.transform;
		_cameraGameObject.AddComponent<Camera>();

		return _cameraGameObject.transform;
	}

	private static BackgroundRenderer SetUpCameraObject(Camera iSetupCamera)
	{
		// Make sure camera is placed properly.
		iSetupCamera.transform.localPosition = Vector3.zero;
		iSetupCamera.transform.localScale = Vector3.one;
		iSetupCamera.transform.localEulerAngles = Vector3.zero;

		// Note: Avoided CopyFrom since main camera setting can change and include something that will break mask camera.
		iSetupCamera.orthographic = true;
		iSetupCamera.clearFlags = CameraClearFlags.Color;
		iSetupCamera.backgroundColor = Color.black;
		iSetupCamera.depth = 10;
		iSetupCamera.allowHDR = false;
		iSetupCamera.allowMSAA = false;
		iSetupCamera.farClipPlane = 10;
		iSetupCamera.nearClipPlane = -10;

		// Get or add processor component.
		var _processor = iSetupCamera.gameObject.GetComponent<BackgroundRenderer>();

		if (_processor == null)
			_processor = iSetupCamera.gameObject.AddComponent<BackgroundRenderer>();

		return _processor;
	}

	private void Awake()
	{
		mMaskCamera = gameObject.GetComponent<Camera>();
	}
}