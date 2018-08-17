using System;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FovMaskRenderer : MonoBehaviour
{
	private const string MaskCameraName = "Mask Camera";

	/// <summary>
	/// Important: Shader to use to render Un Obscured layers.
	/// </summary>
	private const string ReplacementShaderName = "Custom/Fov Mask";

	private Camera mMaskCamera;
	private RenderTexture mMask;

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

	public static FovMaskRenderer InitializeMaskRenderer(
		GameObject iRoot,
		string iShroudLayerName,
		string[] iUnObscuredLayers)
	{
		// Get or create base camera.
		var _cameraTransform = iRoot.transform.Find(MaskCameraName);

		if (_cameraTransform == null)
			_cameraTransform = CreateNewCameraGo(iRoot, MaskCameraName);

		var _maskCamera = _cameraTransform.GetComponent<Camera>();

		if (_maskCamera == null)
			throw new Exception("FovMaskProcessor Unable to properly initialize mask camera.");

		// Setup camera based on main camera.
		var _maskProcessor = SetUpCameraObject(_maskCamera, iShroudLayerName, iUnObscuredLayers);

		return _maskProcessor;
	} 

	public void ResetRenderingTextures(MaskParameters iParameters)
	{
		// Prepare and assign RenderTexture.
		int _textureWidth = iParameters.screenSize.x;
		int _textureHeight = iParameters.screenSize.y;

		var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Raw Occlusion Mask";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		// Note: Assignment will release previous texture if exist.
		mask = _newRenderTexture;

		mMaskCamera.orthographicSize = iParameters.cameraOrthographicSize;
	}

	private static FovMaskRenderer SetUpCameraObject(
		Camera iSetupCamera,
		string iMaskLayerName,
		string[] iUnObscuredLayers)
	{
		// Make sure camera is placed properly.
		iSetupCamera.transform.localPosition = Vector3.zero;
		iSetupCamera.transform.localScale = Vector3.one;
		iSetupCamera.transform.localEulerAngles = Vector3.zero;

		// Note: Avoided CopyFrom since main camera setting can change and include something that will break mask camera.
		var _renderLayers = iUnObscuredLayers.ToList();
		_renderLayers.Add(iMaskLayerName);

		iSetupCamera.orthographic = true;
		iSetupCamera.cullingMask = LayerMask.GetMask(_renderLayers.ToArray()); 
		iSetupCamera.clearFlags = CameraClearFlags.Color;
		iSetupCamera.backgroundColor = Color.black;
		iSetupCamera.depth = 10;
		iSetupCamera.allowHDR = false;
		iSetupCamera.allowMSAA = false;
		iSetupCamera.farClipPlane = 3f;
		iSetupCamera.SetReplacementShader(Shader.Find(ReplacementShaderName), string.Empty);

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

	private void OnPostRender()
	{
		// Notify clients that mask is rendered.
		MaskRendered?.Invoke(mask);
	}
}