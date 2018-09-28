using System;
using UnityEngine;

/// <summary>
/// Lighting System manager. 
/// Orchestrates Mask Renderers and Post Processor to apply lighting to the game scene.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LightingSystem : MonoBehaviour
{
	/// <summary>
	/// FoV Projector position offset in local space.
	/// Assumption that it may be changed during game. Otherwise it should be moved in to rendering settings.
	/// </summary>
	public Vector3 fovCenterOffset;

	public float fovDistance;

	public RenderSettings.Quality quality;

	public RenderSettings renderSettings;
	public MaterialContainer materialContainer;

	private Camera mMainCamera;
	private OcclusionMaskRenderer mOcclusionMaskRenderer;
	private LightMaskRenderer mLightMaskRenderer;
	private BackgroundRenderer mBackgroundRenderer;
	private PostProcessingStack mPostProcessingStack;
	private RenderTexture mGlobalOcclusionMask;
	private RenderTexture mOcclusionMaskExtended;
	private RenderTexture mMixedLightMask;
	private RenderTexture mObstacleLightMask;

	private MaskParameters mCurrentMaskParameters;

	// Note: globalOcclusionMask and occlusionMaskExtended are shader seters.
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

	private RenderTexture occlusionMaskExtended
	{
		get
		{
			return mOcclusionMaskExtended;
		}

		set
		{
			if (mOcclusionMaskExtended == value)
				return;

			if (mOcclusionMaskExtended != null)
			{
				mOcclusionMaskExtended.Release();
			}

			mOcclusionMaskExtended = value;

			Shader.SetGlobalTexture("_FovExtendedMask", value);
		}
	}

	private RenderTexture mixedLightMask
	{
		get
		{
			return mMixedLightMask;
		}

		set
		{
			if (mMixedLightMask == value)
				return;

			if (mMixedLightMask != null)
			{
				mMixedLightMask.Release();
			}

			mMixedLightMask = value;
		}
	}
	
	private RenderTexture obstacleLightMask
	{
		get
		{
			return mObstacleLightMask;
		}

		set
		{
			if (mObstacleLightMask == value)
				return;

			if (mObstacleLightMask != null)
			{
				mObstacleLightMask.Release();
			}

			mObstacleLightMask = value;
		}
	}

	private static void ValidateMainCamera(Camera iMainCamera, RenderSettings iRenderSettings)
	{
		if (iMainCamera.backgroundColor.a > 0)
		{
			UnityEngine.Debug.Log("FovSystem Camera Validation: Camera backgroundColor.a must be 0. This is required to create background mask. Adjusted...");

			iMainCamera.backgroundColor = new Color(iMainCamera.backgroundColor.r, iMainCamera.backgroundColor.g, iMainCamera.backgroundColor.b, 0);
		}

		if (((LayerMask)iMainCamera.cullingMask).HasAny(iRenderSettings.lightSourceLayers))
		{
			UnityEngine.Debug.Log("FovSystem Camera Validation: Camera does not cull one of Light Source Layers! Light System may not work currently.");
		}

		if (((LayerMask)iMainCamera.cullingMask).HasAny(iRenderSettings.backgroundLayers))
		{
			UnityEngine.Debug.Log("FovSystem Camera Validation: Camera does not cull one of Background Layers! Light System wound be able to mask background and would not work correctly.");
		}
	}

	private void OnEnable()
	{
		// Initialize members.
		mMainCamera = gameObject.GetComponent<Camera>();

		if (mMainCamera == null)
			throw new Exception("FovSystemManager require Camera component to operate.");

		ValidateMainCamera(mMainCamera, renderSettings);

		if (mOcclusionMaskRenderer == null)
		{
			mOcclusionMaskRenderer = OcclusionMaskRenderer.InitializeMaskRenderer(gameObject, renderSettings.occlusionLayers, materialContainer.OcclusionMaskShader);
		}

		if (mLightMaskRenderer == null)
		{
			mLightMaskRenderer = LightMaskRenderer.InitializeMaskRenderer(gameObject);
		}

		if (mPostProcessingStack == null)
		{
			mPostProcessingStack = new PostProcessingStack(materialContainer);
		}

		if (mBackgroundRenderer == null)
		{
			mBackgroundRenderer = BackgroundRenderer.InitializeMaskRenderer(gameObject);
		}
	}

	private void OnDisable()
	{
		// Set global occlusion white, so occlusion dependent shaders will show appropriately while system is off.
		Shader.SetGlobalTexture("_FovMask", Texture2D.whiteTexture);

		// Default parameters to force parameters update on enable.
		mCurrentMaskParameters = default(MaskParameters);
	}

	private void Update()
	{
		// Drive render quality from light system inspector.
		renderSettings.quality = quality;

		// Monitor state to detect when we should trigger reinitialization of rendering textures.
		var _newParameters = new MaskParameters(mMainCamera, renderSettings);

		bool _shouldReinitializeTextures = _newParameters != mCurrentMaskParameters;

		if (_shouldReinitializeTextures)
		{
			mCurrentMaskParameters = _newParameters;

			ResetRenderingTextures(mCurrentMaskParameters);
		}
	}	

	private void ResetRenderingTextures(MaskParameters iParameters)
	{
		// Prepare render textures.
		globalOcclusionMask = new RenderTexture(Screen.width, Screen.height, 0)
			                      {
				                      name = "Processed Occlusion Mask"
			                      };

		occlusionMaskExtended = new RenderTexture(iParameters.extendedTextureSize.x, iParameters.extendedTextureSize.y, 0)
			                        {
				                        name = "Processed Extended Occlusion Mask"
			                        };

		mixedLightMask = new RenderTexture(Screen.width, Screen.height, 0)
			                {
								name = "Mixed Light Mask"
			                };

		obstacleLightMask = new RenderTexture((int)(iParameters.lightTextureSize.x * iParameters.wallTextureRescale), (int)(iParameters.lightTextureSize.y * iParameters.wallTextureRescale), 0)
			                    {
									name = "Light Mask"
			                    };

		// Let members handle their own textures.
		// Possibly move to container?
		mOcclusionMaskRenderer.ResetRenderingTextures(iParameters);
		mLightMaskRenderer.ResetRenderingTextures(iParameters);
		mPostProcessingStack.ResetRenderingTextures(iParameters);
		mBackgroundRenderer.ResetRenderingTextures(iParameters);
	}

	private void OnPreRender()
	{
		RenderTexture _rawOcclusionMask;

		using (new DisposableProfiler("1. Occlusion Mask Render (No Gfx Time)"))
		{
			_rawOcclusionMask = mOcclusionMaskRenderer.Render();
		}

		using (new DisposableProfiler("2. Generate FoV"))
		{
			// Note: Next steps will result in Two masks: Generated "Extended Occlusion Mask" will be stored for later use in Light Mixing,
			// and second "Fit Occlusion Mask" will be created next and used during scene rendering.
			Vector3 _fovCenterInWorldSpace = transform.TransformPoint(fovCenterOffset);
			Vector3 _fovCenterOffsetInViewSpace = mMainCamera.WorldToViewportPoint(_fovCenterInWorldSpace) - new Vector3(0.5f, 0.5f, 0);
			Vector3 _fovCenterOffsetInExtendedViewSpace = _fovCenterOffsetInViewSpace * (float)mCurrentMaskParameters.cameraOrthographicSize / mCurrentMaskParameters.extendedCameraSize;

			mPostProcessingStack.GenerateFovMask(_rawOcclusionMask, occlusionMaskExtended, renderSettings, _fovCenterOffsetInExtendedViewSpace, fovDistance, mCurrentMaskParameters);
		}

		using (new DisposableProfiler("3. Fit Occlusion Mask"))
		{
			// Note: Fit Occlusion Mask is cut from "Extended Occlusion Mask" to be used in Occlusion affected shaders during scene render.
			mPostProcessingStack.FitExtendedOcclusionMask(occlusionMaskExtended, globalOcclusionMask, mCurrentMaskParameters);
		}

		using (new DisposableProfiler("4. Blur Fit Occlusion Mask"))
		{
			// Note: This blur is used only with shaders during scene render, so 1 pass should be enough.
			mPostProcessingStack.BlurOcclusionMask(globalOcclusionMask, renderSettings, mCurrentMaskParameters.cameraOrthographicSize);
		}

		// Note: After execution of this method, MainCamera.Render will be executed and scene will be drawn.
	}

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

		RenderTexture _lightMask = null;

		using (new DisposableProfiler("5. Light Mask Render (No Gfx Time)"))
		{
			_lightMask = mLightMaskRenderer.Render(renderSettings);
		}

		using (new DisposableProfiler("6. Generate Obstacle Light Mask"))
		{
			mPostProcessingStack.CreateWallLightMask(_lightMask, obstacleLightMask, renderSettings, mCurrentMaskParameters.cameraOrthographicSize);
		}
		
		// Debug View Selection.
		if (renderSettings.viewMode == RenderSettings.ViewMode.LightLayer)
		{
			Graphics.Blit(_lightMask, iDestination);
			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.WallLayer)
		{
			Graphics.Blit(obstacleLightMask, iDestination);
			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.FovObstacle)
		{
			Graphics.Blit(globalOcclusionMask, iDestination);
			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.FovObstacleExtended)
		{
			Graphics.Blit(occlusionMaskExtended, iDestination);
			return;
		}

		using (new DisposableProfiler("7. Light Mask Blur"))
		{
			mPostProcessingStack.BlurLightMask(_lightMask, renderSettings, mCurrentMaskParameters.cameraOrthographicSize);
		}

		using (new DisposableProfiler("8. Mix Light Masks"))
		{
			// Mix Fov and Light masks.
			var _fovLightMixMaterial = materialContainer.MaskMixerMaterial;
			_fovLightMixMaterial.SetTexture("_LightMask", _lightMask);
			_fovLightMixMaterial.SetTexture("_OcclusionMask", globalOcclusionMask);
			_fovLightMixMaterial.SetTexture("_ObstacleLightMask", obstacleLightMask);

			if (globalOcclusionMask != null)
			{
				_fovLightMixMaterial.SetFloat("_OcclusionUVAdjustment", RenderSettings.GetOcclusionUvAdjustment(_lightMask.width));
			}

			Graphics.Blit(null, mixedLightMask, _fovLightMixMaterial);
		}

		RenderTexture _backgroundMask = null;

		using (new DisposableProfiler("9. Render Background"))
		{
			_backgroundMask = mBackgroundRenderer.Render(renderSettings);
		}

		// Debug Views Selection.
		if (renderSettings.viewMode == RenderSettings.ViewMode.LightMix)
		{
			Graphics.Blit(mixedLightMask, iDestination);
			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.LightLayerBlurred)
		{
			Graphics.Blit(_lightMask, iDestination);
			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.Background)
		{
			Graphics.Blit(_backgroundMask, iDestination);
			return;
		}

		using (new DisposableProfiler("10. Blit Scene with Mixed Lights"))
		{
			var _blitMaterial = materialContainer.blitMaterial;
			_blitMaterial.SetTexture("_LightTex", mixedLightMask);
			_blitMaterial.SetTexture("_BackgroundTex", _backgroundMask);
			_blitMaterial.SetVector("_AmbLightBloomSA", new Vector4(renderSettings.ambient, renderSettings.lightMultiplier, renderSettings.bloomSensitivity, renderSettings.bloomAdd));
			_blitMaterial.SetFloat("_BackgroundMultiplier", renderSettings.backgroundMultiplier);

			Graphics.Blit(iSource, iDestination, _blitMaterial);
		}
	}
}