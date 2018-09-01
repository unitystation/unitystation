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
	private const string LightLayerName = "LightingSource";

	private Camera mMainCamera;
	private FovMaskRenderer mFovMaskRenderer;
	private LightMaskRenderer mLightMaskRenderer;
	private PostProcessingStack mPostProcessingStack;
	private MaskParameters mCurrentMaskParameters;
	private RenderTexture mGlobalOcclusionMask;

	// TODO Refactor. MUST RELEASE OLD MASKS.
	private RenderTexture mGlobalLightMask;
	private RenderTexture compressedMask;
	private RenderTexture mGlobalOcclusionExtendedMask;
	private RenderTexture compressedLightMask;

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

	private RenderTexture globalOcclusionExtendedMask
	{
		get
		{
			return mGlobalOcclusionExtendedMask;
		}

		set
		{
			if (mGlobalOcclusionExtendedMask == value)
				return;

			if (mGlobalOcclusionExtendedMask != null)
			{
				mGlobalOcclusionMask.Release();
			}

			mGlobalOcclusionExtendedMask = value;

			Shader.SetGlobalTexture("_FovExtendedMask", value);
		}
	}

	private RenderTexture globalLightMask
	{
		get
		{
			return mGlobalLightMask;
		}

		set
		{
			if (mGlobalLightMask == value)
				return;

			if (mGlobalLightMask != null)
			{
				mGlobalLightMask.Release();
			}

			mGlobalLightMask = value;

			Shader.SetGlobalTexture("_LightMask", value);
		}
	}

	private void OnEnable()
	{
		mMainCamera = gameObject.GetComponent<Camera>();

		if (mMainCamera == null)
			throw new Exception("FovSystemManager require Camera component to operate.");

		if (mFovMaskRenderer == null)
		{
			mFovMaskRenderer = FovMaskRenderer.InitializeMaskRenderer(gameObject, MaskLayerName, renderSettings.unObscuredLayers);
			mFovMaskRenderer.MaskRendered += OnMaskRendered;
		}

		if (mLightMaskRenderer == null)
		{
			mLightMaskRenderer = LightMaskRenderer.InitializeMaskRenderer(gameObject, LightLayerName, () => renderSettings);
			mLightMaskRenderer.MaskRendered += OnLightMaskRendered;
		}

		if (mPostProcessingStack == null)
		{
			mPostProcessingStack = new PostProcessingStack(materialContainer);
		}
	}

	private void OnLightMaskRendered(RenderTexture iMask)
	{
		// Light mask is rendered witch extended camera scale and must be fitted back in to render camera scale.
		// Note: Light mask is NOT rendered with extended texture size as obstacle mask.
		//Vector2 _scale = new Vector2((float)iMask.width / globalOcclusionExtendedMask.width, (float)iMask.height / globalOcclusionExtendedMask.height);
		//
		//Graphics.Blit(iMask, compressedLightMask, _scale, (Vector2.one - _scale) * 0.5f);

		mPostProcessingStack.PostProcessLightMask(iMask, globalLightMask, renderSettings);
	}

	private void Update()
	{
		// Monitor state to detect when we should re-create rendering textures.
		var _newParameters = new MaskParameters(mMainCamera, renderSettings);

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

		compressedMask = new RenderTexture(Screen.width, Screen.height, 0);

		globalLightMask = new RenderTexture(Screen.width, Screen.height, 0);
		globalLightMask.name = "Processed Light Mask";

		globalOcclusionExtendedMask = new RenderTexture(iParameters.extendedTextureSize.x, iParameters.extendedTextureSize.y, 0);

		compressedLightMask = new RenderTexture(Screen.width, Screen.height, 0);



		mFovMaskRenderer.ResetRenderingTextures(iParameters);
		mLightMaskRenderer.ResetRenderingTextures(iParameters);
		mPostProcessingStack.ResetRenderingTextures(iParameters);
	}

	/// <summary>
	/// Fires when new mask is provided. 
	/// Runs mask thru post processing stack and stores it for later use in main rendering loop and post render blit.
	/// </summary>
	/// <param name="iMask">Occlusion mask with R and G channels.</param>
	private void OnMaskRendered(RenderTexture iMask)
	{
		if (enabled == false)
			return;

		globalOcclusionExtendedMask = iMask;
		//Graphics.Blit(iMask, globalOcclusionExtendedMask, materialContainer.fovMaterial);

		// Mask is rendered in up-scaled resolution and must be fitted in to screen resolution to be usable for the rest of the system.
		// Fit upscale mask.
		Vector2 _scale = new Vector2((float)compressedMask.width / iMask.width, (float)compressedMask.height / iMask.height);

		Graphics.Blit(iMask, compressedMask, _scale, (Vector2.one - _scale) * 0.5f);

		mPostProcessingStack.PostProcessMask(compressedMask, globalOcclusionMask, renderSettings);
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
		materialContainer.blitMaterial.SetTexture("_LightMask", mGlobalLightMask);
		materialContainer.blitMaterial.SetTexture("_Mask", mGlobalOcclusionMask);
		materialContainer.blitMaterial.SetFloat("_Ambient", renderSettings.ambient);
		materialContainer.blitMaterial.SetFloat("_LightMultiplier", renderSettings.lightMultiplier);
		Graphics.Blit(iSource, iDestination, materialContainer.blitMaterial);
	}
}