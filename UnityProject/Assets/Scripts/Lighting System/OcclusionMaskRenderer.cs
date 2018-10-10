using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OcclusionMaskRenderer : MonoBehaviour, ITextureRenderer
{
	private const string MaskCameraName = "Obstacle Mask Camera";

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

	public static OcclusionMaskRenderer InitializeMaskRenderer(
		GameObject iRoot,
		LayerMask iOcclusionLayers,
		Shader iReplacementShader)
	{
		// Get or create base camera.
		var _cameraTransform = iRoot.transform.Find(MaskCameraName);

		if (_cameraTransform == null)
			_cameraTransform = CreateNewCameraGo(iRoot, MaskCameraName);

		var _maskCamera = _cameraTransform.GetComponent<Camera>();

		if (_maskCamera == null)
			throw new Exception("FovMaskProcessor Unable to properly initialize mask camera.");

		// Setup camera based on main camera.
		var _maskProcessor = SetUpCameraObject(_maskCamera, iOcclusionLayers, iReplacementShader);

		return _maskProcessor;
	} 

	public void ResetRenderingTextures(OperationParameters iParameters)
	{
		var _newRenderTexture = new RenderTexture(iParameters.occlusionPPRTParameter.resolution.x, iParameters.occlusionPPRTParameter.resolution.y, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Raw Scaled Occlusion Mask";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		_newRenderTexture.antiAliasing = 1;
		_newRenderTexture.filterMode = FilterMode.Point;

		// Note: Assignment will release previous texture if exist.
		mask = _newRenderTexture;

		mMaskCamera.orthographicSize = iParameters.extendedCameraSize;
	}

	public PixelPerfectRTP Render(Camera iCameraToMatch, PixelPerfectRTParameter iPPRTParameter)
	{
		// Arrange.
		mMaskCamera.enabled = false;
		mMaskCamera.backgroundColor = new Color(0, 0, 0, 0);
		var _renderPosition = iPPRTParameter.GetRendererPosition(iCameraToMatch.transform.position);
		mMaskCamera.transform.position = _renderPosition;

		SetViewPort(iPPRTParameter.GetRendererViewport());


		// Execute.
		mMaskCamera.Render();

		// Wrap.
		var pprt = new PixelPerfectRTP(iPPRTParameter, mask, mMaskCamera.transform.position);

		return pprt;
	}

	private void SetViewPort(Vector2 iViewport)
	{
		mMaskCamera.rect = new Rect((1 - iViewport.x) * 0.5f, 0, iViewport.x, 1);
		mMaskCamera.orthographicSize = iViewport.y * 0.5f;
	}

	private static OcclusionMaskRenderer SetUpCameraObject(
		Camera iSetupCamera,
		LayerMask iOcclusionLayers,
		Shader iReplacementShader)
	{
		// Make sure camera is placed properly.
		iSetupCamera.transform.localPosition = Vector3.zero;
		iSetupCamera.transform.localScale = Vector3.one;
		iSetupCamera.transform.localEulerAngles = Vector3.zero;

		iSetupCamera.orthographic = true;
		iSetupCamera.cullingMask = iOcclusionLayers; 
		iSetupCamera.clearFlags = CameraClearFlags.Color;
		iSetupCamera.backgroundColor = Color.black;
		iSetupCamera.depth = 9;
		iSetupCamera.allowHDR = false;

		iSetupCamera.nearClipPlane = -3f;
		iSetupCamera.farClipPlane = 3f;
		iSetupCamera.SetReplacementShader(iReplacementShader, string.Empty);

		// Get or add processor component.
		var _processor = iSetupCamera.gameObject.GetComponent<OcclusionMaskRenderer>();

		if (_processor == null)
			_processor = iSetupCamera.gameObject.AddComponent<OcclusionMaskRenderer>();

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
}