using System;
using UnityEngine;

/// <summary>
/// Post processes main camera renderer by adding FoV Mask on top of final image and provides global mask to use in shaders.
/// 
/// System works by rendering FoV Stencil and Un-Obscured layers with a separate camera.
/// FoV Stencil and Un-Obscured are rendered in to two separate channels:
/// Channel R - not blurred mask. Used for Un-Obscured layers.
/// Channel G - blurred mask. Used for FoV Stencil.
/// Resulted mask is post processed with blur and pixelation passes and then stored to use for:
/// 1. Provide global accessible mask to use in shaders. Currently it is used to alpha hide objects under a shroud.
/// 2. Blit mask in to final rendered image.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FovSystem : MonoBehaviour
{
	public FovRenderSettings renderSettings;
	public FovMaterialContainer materialContainer;

	private const string MaskLayerName = "FieldOfViewMask";

	private Camera mMainCamera;
	private FovMaskRenderer mFovMaskRenderer;
	private PostProcessingStack mPostProcessingStack;
	private MaskParameters mCurrentMaskParameters;
	private RenderTexture mGlobalOcclusionMask;

	private RenderTexture globalOcclusionMask
	{
		get
		{
			return mGlobalOcclusionMask;
		}

		set
		{
			if (mGlobalOcclusionMask == value)
				return;

			if (mGlobalOcclusionMask != null)
			{
				mGlobalOcclusionMask.Release();
			}

			mGlobalOcclusionMask = value;

			Shader.SetGlobalTexture("_FovMask", value);
		}
	}

	private void Awake()
	{
		mMainCamera = gameObject.GetComponent<Camera>();

		if (mMainCamera == null)
			throw new Exception("FovSystemManager require Camera component to operate.");

		mFovMaskRenderer = FovMaskRenderer.InitializeMaskRenderer(gameObject, MaskLayerName, renderSettings.unObscuredLayers);
		mFovMaskRenderer.MaskRendered += OnMaskRendered;

		mPostProcessingStack = new PostProcessingStack(materialContainer);
	}

	private void Update()
	{
		// Monitor state to detect when we should re-create rendering textures.
		var _newParameters = new MaskParameters(mMainCamera);

		bool _shouldUpdateTextureBuffers = _newParameters != mCurrentMaskParameters;

		if (_shouldUpdateTextureBuffers)
		{
			mCurrentMaskParameters = _newParameters;

			ResetRenderingTextures(mCurrentMaskParameters);
		}
	}	

	private void ResetRenderingTextures(MaskParameters iParameters)
	{
		globalOcclusionMask = new RenderTexture(Screen.width, Screen.height, 0);
		globalOcclusionMask.name = "Processed Occlusion Mask";

		mFovMaskRenderer.ResetRenderingTextures(iParameters);
		mPostProcessingStack.ResetRenderingTextures(iParameters);
	}

	/// <summary>
	/// Fires when new mask is provided. 
	/// Runs mask thru post processing stack and stores it for later use in main rendering loop and post render blit.
	/// </summary>
	/// <param name="iMask">Occlusion mask with R and G channels.</param>
	private void OnMaskRendered(RenderTexture iMask)
	{
		mPostProcessingStack.PostProcessMask(iMask, globalOcclusionMask, renderSettings);
	}

	/// <summary>
	/// Blit processed mask with rendered scene.
	/// </summary>
	private void OnRenderImage(RenderTexture iSource, RenderTexture iDestination)
	{
		if (materialContainer.blitMaterial == null)
		{
			Debug.Log($"FovSystemManager: Unable to blit Fov mask. {nameof(materialContainer.blitMaterial)} not provided.");
			return;
		}

		if (globalOcclusionMask == null)
		{
			Graphics.Blit(iSource, iDestination);
			return;
		}

		// Blit already processed mask in to rendered scene.
		materialContainer.blitMaterial.SetTexture("_Mask", mGlobalOcclusionMask);
		materialContainer.blitMaterial.SetFloat("_Alpha", renderSettings.maskAlpha);
		Graphics.Blit(iSource, iDestination, materialContainer.blitMaterial);
	}
}