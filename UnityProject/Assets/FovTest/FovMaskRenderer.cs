using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FovMaskRenderer : MonoBehaviour
{
	private Camera mMaskCamera;
	private RenderTexture mMask;
	private Func<FovRenderSettings> mRenderSettingsGetter;
	private Camera mSourceCamera;
	private int mLastDownscaling;
	private Vector2 mLastScreenSize;

	public RenderTexture mask
	{
		get
		{
			return mMask;
		}

		private set
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

	private FovRenderSettings renderSettings
	{
		get
		{
			if (mRenderSettingsGetter == null)
			{
				throw new Exception("FovMaskRenderer: Unable to provide render settings to members. FovMaskRenderer wasn't injected with setting getter. FovMaskRenderer should be initialized thru InitializeMaskRenderer.");
			}

			return mRenderSettingsGetter();
		}
	}

	public static FovMaskRenderer InitializeMaskRenderer(
		GameObject iRoot,
		Camera iSourceCamera,
		string iLayerName,
		string iCameraName,
		Func<FovRenderSettings> iRenderSettingGetter)
	{
		// Get or create base camera.
		var _cameraTransform = iRoot.transform.Find(iCameraName);

		if (_cameraTransform == null)
			_cameraTransform = CreateNewCameraGo(iRoot, iCameraName);

		var _maskCamera = _cameraTransform.GetComponent<Camera>();

		if (_maskCamera == null)
			throw new Exception("FovMaskProcessor Unable to properly initialize mask camera.");

		// Setup camera based on main camera.
		var _maskProcessor = SetUpCameraObject(_maskCamera, iSourceCamera, iLayerName);

		_maskProcessor.mRenderSettingsGetter = iRenderSettingGetter;
		_maskProcessor.mSourceCamera = iSourceCamera;

		return _maskProcessor;
	} 

	private static FovMaskRenderer SetUpCameraObject(Camera iSetupCamera, Camera iSourceCamera, string iMaskLayerName)
	{
		// Make sure camera is placed properly.
		iSetupCamera.transform.localPosition = Vector3.zero;
		iSetupCamera.transform.localScale = Vector3.one;
		iSetupCamera.transform.localEulerAngles = Vector3.zero;

		// Note: Avoided CopyFrom since main camera setting can change and include something that will break mask camera.
		iSetupCamera.orthographic = true;
		iSetupCamera.orthographicSize = iSourceCamera.orthographicSize;
		iSetupCamera.cullingMask = LayerMask.GetMask(new[] { iMaskLayerName, "Walls", "Door Closed" });
		iSetupCamera.clearFlags = CameraClearFlags.Color;
		iSetupCamera.backgroundColor = Color.black;
		iSetupCamera.depth = 10;
		iSetupCamera.SetReplacementShader(Shader.Find("Custom/Fov Mask"), string.Empty);

		// Get or add processor component.
		var _processor = iSetupCamera.gameObject.GetComponent<FovMaskRenderer>();

		if (_processor == null)
			_processor = iSetupCamera.gameObject.AddComponent<FovMaskRenderer>();

		return _processor;
	}

	private static Transform CreateNewCameraGo(GameObject iRoot, string iMaskLayerName)
	{
		var _cameraGameObject = new GameObject(iMaskLayerName);
		_cameraGameObject.transform.parent = iRoot.transform;

		_cameraGameObject.AddComponent<Camera>();

		return _cameraGameObject.transform;
	}

	private void Awake()
	{
		mMaskCamera = gameObject.GetComponent<Camera>();
	}

	private void OnPreCull()
	{
		bool _shouldUpdateRenderTexture = CheckShouldUpdateTexture(renderSettings);

		UpdateCameraSetup();

		if (_shouldUpdateRenderTexture)
		{
			mLastDownscaling = renderSettings.maskDownscaling;
			mLastScreenSize = new Vector2(Screen.width, Screen.height);

			// Prepare and assign RenderTexture.
			int _textureWidth = Screen.width >> renderSettings.maskDownscaling;
			int _textureHeight = Screen.height >> renderSettings.maskDownscaling;

			var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);

			// Note: Assignment will release previous texture if exist.
			mask = _newRenderTexture;
		}
	}

	private bool CheckShouldUpdateTexture(FovRenderSettings iRenderSettings)
	{
		if (mask == null)
			return true;

		if (mMaskCamera.orthographicSize != mSourceCamera.orthographicSize)
			return true;

		if (mLastDownscaling != iRenderSettings.maskDownscaling)
			return true;

		if (mLastScreenSize != new Vector2(Screen.width, Screen.height))
			return true;

		return false;
	}

	private void UpdateCameraSetup()
	{
		if (mSourceCamera == null)
			return;

		mMaskCamera.orthographicSize = mSourceCamera.orthographicSize;
	}
}