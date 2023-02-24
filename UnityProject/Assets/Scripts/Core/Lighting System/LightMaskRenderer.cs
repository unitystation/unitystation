using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class LightMaskRenderer : MonoBehaviour
{
	private const string MaskCameraName = "Light Mask Camera";

	private Camera mMaskCamera;
	private PixelPerfectRT mPPRenderTexture;
	private Vector3 mPreviousCameraPosition;
	private Vector2 mPreviousFilteredPosition;

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

	public PixelPerfectRT Render(
		Camera iCameraToMatch,
		PixelPerfectRTParameter iPPRTParameter,
		PixelPerfectRT iOcclusionMask,
		RenderSettings iRenderSettings,
		bool iMatrixRotationMode)
	{
		// Arrange.
		Vector2 _renderPosition;

		if (iMatrixRotationMode == false)
		{
			_renderPosition = iPPRTParameter.GetFilteredRendererPosition(iCameraToMatch.transform.position, mPreviousCameraPosition, mPreviousFilteredPosition);
		}
		else
		{
			// Note: Do not apply PixelPerfect position when matrix is rotating.
			_renderPosition = iCameraToMatch.transform.position;
		}

		mPreviousCameraPosition = iCameraToMatch.transform.position;
		mPreviousFilteredPosition = _renderPosition;

		mMaskCamera.enabled = false;
		mMaskCamera.backgroundColor = Color.clear;
		mMaskCamera.transform.position = _renderPosition;
		mMaskCamera.orthographicSize = iPPRTParameter.orthographicSize;
		mMaskCamera.cullingMask = iRenderSettings.lightSourceLayers;

		if (mPPRenderTexture == null)
		{
			mPPRenderTexture = new PixelPerfectRT(iPPRTParameter);
			mPPRenderTexture.renderTexture.filterMode = FilterMode.Bilinear;
			mPPRenderTexture.renderTexture.graphicsFormat = GraphicsFormat.R16G16B16A16_UNorm;
		}
		else
		{
			mPPRenderTexture.Update(iPPRTParameter);
		}

		// Arrange Occlusion RT
		iOcclusionMask.renderTexture.filterMode = FilterMode.Bilinear;
		Shader.SetGlobalTexture("_FovExtendedMask", iOcclusionMask.renderTexture);

		// Note: We need to override mLightPPRT position for transformation, because new position for mLightPPRT will be set during light rendering.
		Shader.SetGlobalVector("_FovTransformation", iOcclusionMask.GetTransformation(mPPRenderTexture, _renderPosition));

		// Execute.
		mPPRenderTexture.Render(mMaskCamera);

		return mPPRenderTexture;
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
		iSetupCamera.nearClipPlane = -3f;

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