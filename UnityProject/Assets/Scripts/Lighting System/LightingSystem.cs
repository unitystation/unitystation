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
	//public RenderSettings.Quality quality;
	public RenderSettings renderSettings;
	public MaterialContainer materialContainer;	
	
	private Camera mMainCamera;
	private OcclusionMaskRenderer mOcclusionRenderer;
	private LightMaskRenderer mLightMaskRenderer;
	private BackgroundRenderer mBackgroundRenderer;
	private PostProcessingStack mPostProcessingStack;
	private PixelPerfectRT mGlobalOcclusionMask;
	private PixelPerfectRT mOcclusionMaskExtended;
	private PixelPerfectRT mMixedLightMask;
	private PixelPerfectRT mObstacleLightMask;
	private PixelPerfectRT mOcclusionPPRT;
	private PixelPerfectRT mlightPPRT;
	private bool mDoubleFrameRendererSwitch;

	// Note: globalOcclusionMask and occlusionMaskExtended are shader seters.
	private PixelPerfectRT globalOcclusionMask
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

			Shader.SetGlobalTexture("_FovMask", value.renderTexture);
		}
	}

	private PixelPerfectRT occlusionMaskExtended
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
		}
	}

	private PixelPerfectRT mixedLightMask
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
	
	private PixelPerfectRT obstacleLightMask
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

	private OperationParameters operationParameters { get; set; }

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

		if (mOcclusionRenderer == null)
		{
			mOcclusionRenderer = OcclusionMaskRenderer.InitializeMaskRenderer(gameObject, renderSettings.occlusionLayers, materialContainer.OcclusionMaskShader);
		}

		if (mLightMaskRenderer == null)
		{
			mLightMaskRenderer = LightMaskRenderer.InitializeMaskRenderer(gameObject);
		}

		if (mBackgroundRenderer == null)
		{
			mBackgroundRenderer = BackgroundRenderer.InitializeMaskRenderer(gameObject);
		}

		if (mPostProcessingStack == null)
		{
			mPostProcessingStack = new PostProcessingStack(materialContainer);
		}
	}

	private void OnDisable()
	{
		// Set global occlusion white, so occlusion dependent shaders will show appropriately while system is off.
		Shader.SetGlobalTexture("_FovMask", Texture2D.whiteTexture);

		// Default parameters to force parameters update on enable.
		operationParameters = default(OperationParameters);
	}

	private void Update()
	{
		// Monitor state to detect when we should trigger reinitialization of rendering textures.
		var _newParameters = new OperationParameters(mMainCamera, renderSettings);

		bool _shouldReinitializeTextures = _newParameters != operationParameters;

		if (_shouldReinitializeTextures)
		{
			operationParameters = _newParameters;

			ResolveRenderingTextures(operationParameters);
		}
	}	

	private void ResolveRenderingTextures(OperationParameters iParameters)
	{
		// Prepare render textures.
		occlusionMaskExtended = new PixelPerfectRT(operationParameters.fovPPRTParameter);

		globalOcclusionMask = new PixelPerfectRT(operationParameters.lightPPRTParameter);

		mixedLightMask = new PixelPerfectRT(operationParameters.lightPPRTParameter);
		mixedLightMask.renderTexture.filterMode = FilterMode.Point;

		obstacleLightMask = new PixelPerfectRT(operationParameters.obstacleLightPPRTParameter);

		// Let members handle their own textures.
		// Possibly move to container?
		mPostProcessingStack.ResetRenderingTextures(iParameters);
		mBackgroundRenderer.ResetRenderingTextures(iParameters);
	}

	private void OnPreRender()
	{
		if (renderSettings.doubleFrameRenderingMode && mDoubleFrameRendererSwitch == false)
		{
			Shader.SetGlobalVector("_FovMaskTransformation", globalOcclusionMask.GetTransformation(mMainCamera));
			return;
		}

		using (new DisposableProfiler("1. Occlusion Mask Render (No Gfx Time)"))
		{
			mOcclusionPPRT = mOcclusionRenderer.Render(mMainCamera, operationParameters.occlusionPPRTParameter);
		}

		using (new DisposableProfiler("2. Generate FoV"))
		{
			if (occlusionMaskExtended == null)
			{
				occlusionMaskExtended = new PixelPerfectRT(operationParameters.fovPPRTParameter);
			}
			else
			{
				occlusionMaskExtended.Update(operationParameters.fovPPRTParameter);
			}

			// Note: Next steps will result in Two masks: Generated "Extended Occlusion Mask" will be stored for later use in Light Mixing,
			// and second "Fit Occlusion Mask" will be created next and used during scene rendering.
			Vector3 _fovCenterInWorldSpace = transform.TransformPoint(fovCenterOffset);
			Vector3 _fovCenterOffsetInViewSpace = mMainCamera.WorldToViewportPoint(_fovCenterInWorldSpace) - new Vector3(0.5f, 0.5f, 0);
			Vector3 _fovCenterOffsetInExtendedViewSpace = _fovCenterOffsetInViewSpace * (float)operationParameters.cameraOrthographicSize / mOcclusionPPRT.orthographicSize;
			
			mPostProcessingStack.GenerateFovMask(mOcclusionPPRT, occlusionMaskExtended, renderSettings, _fovCenterOffsetInExtendedViewSpace, fovDistance, operationParameters);
		}

		using (new DisposableProfiler("3. Fit Occlusion Mask"))
		{
			globalOcclusionMask.Update(operationParameters.lightPPRTParameter);

			// Note: Fit Occlusion Mask is cut from "Extended Occlusion Mask" to be used in Occlusion affected shaders during scene render.
			PixelPerfectRT.Transform(occlusionMaskExtended, globalOcclusionMask, materialContainer.PPRTTransformMaterial);

			// Update shader global transformation for occlusionMaskExtended so sprites can transform mask correctly.
			Shader.SetGlobalVector("_FovMaskTransformation", globalOcclusionMask.GetTransformation(mMainCamera));
		}

		using (new DisposableProfiler("4. Blur Fit Occlusion Mask"))
		{
			// Note: This blur is used only with shaders during scene render, so 1 pass should be enough.
			mPostProcessingStack.BlurOcclusionMask(globalOcclusionMask.renderTexture, renderSettings, operationParameters.cameraOrthographicSize);
		}

		// Note: After execution of this method, MainCamera.Render will be executed and scene will be drawn.
	}

	private void OnRenderImage(RenderTexture iSource, RenderTexture iDestination)
	{
		if (renderSettings.doubleFrameRenderingMode)
		{
			if (mDoubleFrameRendererSwitch)
			{
				var _blitMaterial = materialContainer.blitMaterial;
				_blitMaterial.SetVector("_LightTransform", mlightPPRT.GetTransformation(mMainCamera));
				_blitMaterial.SetVector("_OcclusionTransform", occlusionMaskExtended.GetTransformation(mMainCamera));

				Graphics.Blit(iSource, iDestination, _blitMaterial);

				mDoubleFrameRendererSwitch = false;
				return;
			}

			mDoubleFrameRendererSwitch = true;
		}

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

		using (new DisposableProfiler("5. Light Mask Render (No Gfx Time)"))
		{
			mlightPPRT = mLightMaskRenderer.Render(
				mMainCamera,
				operationParameters.lightPPRTParameter,
				occlusionMaskExtended,
				renderSettings);
		}

		using (new DisposableProfiler("6. Generate Obstacle Light Mask"))
		{
			mPostProcessingStack.CreateWallLightMask(
				mlightPPRT,
				obstacleLightMask,
				renderSettings,
				operationParameters.cameraOrthographicSize);
		}
		
		// Debug View Selection.
		if (renderSettings.viewMode == RenderSettings.ViewMode.LightLayer)
		{
			PixelPerfectRT.Transform(mlightPPRT, iDestination, mMainCamera, materialContainer.PPRTTransformMaterial);

			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.WallLayer)
		{
			PixelPerfectRT.Transform(obstacleLightMask, iDestination, mMainCamera, materialContainer.PPRTTransformMaterial);

			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.FovObstacle)
		{
			PixelPerfectRT.Transform(globalOcclusionMask, iDestination, mMainCamera, materialContainer.PPRTTransformMaterial);

			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.FovObstacleExtended)
		{
			PixelPerfectRT.Transform(occlusionMaskExtended, iDestination, mMainCamera, materialContainer.PPRTTransformMaterial);

			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.Obstacle)
		{
			PixelPerfectRT.Transform(mOcclusionPPRT, iDestination, mMainCamera, materialContainer.PPRTTransformMaterial);

			return;
		}

		using (new DisposableProfiler("7. Light Mask Blur"))
		{
			mPostProcessingStack.BlurLightMask(mlightPPRT.renderTexture, renderSettings, operationParameters.cameraOrthographicSize);
		}

		RenderTexture _backgroundMask = null;

		using (new DisposableProfiler("8. Render Background"))
		{
			_backgroundMask = mBackgroundRenderer.Render(renderSettings);
		}

		// Debug Views Selection.
		if (renderSettings.viewMode == RenderSettings.ViewMode.LightLayerBlurred)
		{
			PixelPerfectRT.Transform(mlightPPRT, iDestination, mMainCamera, materialContainer.PPRTTransformMaterial);

			return;
		}
		else if (renderSettings.viewMode == RenderSettings.ViewMode.Background)
		{
			Graphics.Blit(_backgroundMask, iDestination);
			return;
		}

		using (new DisposableProfiler("9. Blit Scene with Mixed Lights"))
		{
			var _blitMaterial = materialContainer.blitMaterial;
			_blitMaterial.SetTexture("_LightMask", mlightPPRT.renderTexture);
			_blitMaterial.SetTexture("_OcclusionMask", occlusionMaskExtended.renderTexture);
			_blitMaterial.SetTexture("_ObstacleLightMask", obstacleLightMask.renderTexture);
			_blitMaterial.SetVector("_LightTransform", mlightPPRT.GetTransformation(mMainCamera));
			_blitMaterial.SetVector("_OcclusionTransform", occlusionMaskExtended.GetTransformation(mMainCamera));

			_blitMaterial.SetTexture("_BackgroundTex", _backgroundMask);
			_blitMaterial.SetVector("_AmbLightBloomSA", new Vector4(renderSettings.ambient, renderSettings.lightMultiplier, renderSettings.bloomSensitivity, renderSettings.bloomAdd));
			_blitMaterial.SetFloat("_BackgroundMultiplier", renderSettings.backgroundMultiplier);

			Graphics.Blit(iSource, iDestination, _blitMaterial);

			mixedLightMask.renderTexture.filterMode = FilterMode.Point;
		}
	}
}