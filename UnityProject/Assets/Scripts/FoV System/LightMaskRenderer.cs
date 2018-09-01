using System;
using System.Linq;
using UnityEngine;

public class LightMaskRenderer : MonoBehaviour
{
	private const string MaskCameraName = "Light Mask Camera";

	private Camera mMaskCamera;
	private RenderTexture mMask;

	private Func<FovRenderSettings> ReadSettings;
	private MaskParameters mMaskParameters;

	public event Action<RenderTexture> MaskRendered;
	
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
		GameObject iRoot,
		string iShroudLayerName,
		Func<FovRenderSettings> iSettingsReader)
	{
		// Get or create base camera.
		var _cameraTransform = iRoot.transform.Find(MaskCameraName);

		if (_cameraTransform == null)
			_cameraTransform = CreateNewCameraGo(iRoot, MaskCameraName);

		var _maskCamera = _cameraTransform.GetComponent<Camera>();

		if (_maskCamera == null)
			throw new Exception("LightMaskRenderer Unable to properly initialize mask camera.");

		// Setup camera based on main camera.
		var _maskProcessor = SetUpCameraObject(_maskCamera, iShroudLayerName);

		_maskProcessor.ReadSettings = iSettingsReader;

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

		// Note: Assignment will release previous texture if exist.
		mask = _newRenderTexture;

		mMaskCamera.orthographicSize = iParameters.cameraOrthographicSize;

		Vector2 _scale = new Vector2((float)iParameters.screenSize.x / iParameters.extendedTextureSize.x, (float)iParameters.screenSize.y / iParameters.extendedTextureSize.y);
		Shader.SetGlobalVector("_ExtendedToSmallTextureScale", _scale);

		mMaskParameters = iParameters;
	}

	private static Transform CreateNewCameraGo(GameObject iRoot, string iMaskLayerName)
	{
		var _cameraGameObject = new GameObject(iMaskLayerName);
		_cameraGameObject.transform.parent = iRoot.transform;
		_cameraGameObject.AddComponent<Camera>();

		return _cameraGameObject.transform;
	}

	private static LightMaskRenderer SetUpCameraObject(
		Camera iSetupCamera,
		string iMaskLayerName)
	{
		// Make sure camera is placed properly.
		iSetupCamera.transform.localPosition = Vector3.zero;
		iSetupCamera.transform.localScale = Vector3.one;
		iSetupCamera.transform.localEulerAngles = Vector3.zero;

		// Note: Avoided CopyFrom since main camera setting can change and include something that will break mask camera.
		iSetupCamera.orthographic = true;
		iSetupCamera.cullingMask = LayerMask.GetMask(iMaskLayerName); 
		iSetupCamera.clearFlags = CameraClearFlags.Color;
		iSetupCamera.backgroundColor = Color.black;
		iSetupCamera.depth = 10;
		iSetupCamera.allowHDR = false;
		iSetupCamera.allowMSAA = false;
		iSetupCamera.farClipPlane = 3f;
		//iSetupCamera.SetReplacementShader(Shader.Find(ReplacementShaderName), string.Empty);

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

	private void OnPreRender()
	{
		// Setup pass shaders.
		var _settings = ReadSettings();

		if (_settings == null)
		{
			UnityEngine.Debug.LogError("LightMaskRenderer: Unable to read settings. ReadSettings wasn't injected.");
			return;
		}

		/*
		float _pixelsPerUnit = 1f / (float)_settings.lightPixelSize;
		Shader.SetGlobalFloat("_PixelsPerBlock", _pixelsPerUnit);

		var rawSmallCamHeight = mMaskParameters.cameraOrthographicSize * 2f * _pixelsPerUnit;

		var _smallLightTextureSize = new Vector2Int(
			Mathf.RoundToInt(rawSmallCamHeight * mMaskParameters.cameraAspect),
			Mathf.RoundToInt(rawSmallCamHeight));

		var rawCamHeight = (mMaskParameters.cameraOrthographicSize + _settings.lightCameraAdd)*2f;
		var rawCamWidth = (mMaskParameters.cameraOrthographicSize *mMaskParameters.cameraAspect + _settings.lightCameraAdd)*2f;

		var _extendedLightTextureSize = new Vector2Int(
			Mathf.RoundToInt(rawCamWidth*_pixelsPerUnit),
			Mathf.RoundToInt(rawCamHeight*_pixelsPerUnit));
		*/

		/*
		Shader.SetGlobalVector("_ExtendedToSmallTextureScale", new Vector2(
			_smallLightTextureSize.x / (float)_extendedLightTextureSize.x,
			_smallLightTextureSize.y / (float)_extendedLightTextureSize.y));
			*/

		//Shader.SetGlobalVector("_PosOffset", mask.texelSize);
	}

	private void OnPostRender()
	{
		// Notify clients that mask is rendered.
		MaskRendered?.Invoke(mask);
	}
}