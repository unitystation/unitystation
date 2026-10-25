using System;
using Logs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace US13.Core.Lighting_System
{
	[DefaultExecutionOrder(10000)]
	[RequireComponent(typeof(Camera))]
	public class LightingSystem : MonoBehaviour
	{
		/// What the renderer feature should do for this camera this frame.
		public enum CompositeMode
		{
			/// Leave the camera output untouched
			None,
			/// Run blitMaterial over the camera color.
			Lit,
			/// Copy DebugHandle over the camera color.
			Debug
		}

		/// <summary>
		/// FoV Projector position offset in local space.
		/// Assumption that it may be changed during game. Otherwise it should be moved in to rendering settings.
		/// </summary>
		public Vector3 fovCenterOffset;

		public float fovDistance;
		public RenderSettings renderSettings;
		public MaterialContainer materialContainer;

		private static Func<Vector3, Vector3, Vector2, Vector2> HandlePPPositionRequest;

		public Camera UICamera;

		public bool DoUICamera = true;

		public bool DoOtherBit = true;

		private Camera mMainCamera;
		private OcclusionMaskRenderer mOcclusionRenderer;
		public ShadowMaskRenderer mShadowMaskRenderer;
		public FulbrightRenderer FulbrightRenderer;
		private LightMaskRenderer mLightMaskRenderer;
		private BackgroundRenderer mBackgroundRenderer;
		private PostProcessingStack mPostProcessingStack;
		private PixelPerfectRT mGlobalOcclusionMask;
		private PixelPerfectRT mFloorOcclusionMask;
		private PixelPerfectRT mWallFloorOcclusionMask;
		private PixelPerfectRT mObstacleLightMask;
		private PixelPerfectRT mOcclusionPPRT;
		private PixelPerfectRT mlightPPRT;
		private PixelPerfectRT mUILayer;

		private bool mDoubleFrameRendererSwitch;
		private bool mMatrixRotationMode;
		private float mMatrixRotationModeBlend;
		//used for FOV checking if async readback is supported
		private TextureDataRequest mTextureDataRequest;
		//used for FOV checking is async readback is NOT supported
		private Texture2D mTex2DWallFloorOcclusionMask;
		private OperationParameters mOperationParameters;

		// Persistent screen-sized targets (replace the per-frame GetTemporary / iDestination usage).
		private RenderTexture mUIRenderTexture;
		private RenderTexture mDebugRenderTexture;
		private RTHandle mDebugHandle;

		// State consumed by LightingRendererFeature.
		public CompositeMode compositeMode { get; private set; } = CompositeMode.None;
		public bool compositeUI { get; private set; }
		public Material compositeMaterial => materialContainer != null ? materialContainer.blitMaterial : null;
		public Material uiCompositeMaterial => materialContainer != null ? materialContainer.uiBlitMaterial : null;
		public RTHandle DebugHandle => mDebugHandle;

		/// <summary>
		/// Lighting system was enabled/disabled
		/// </summary>
		public event System.Action<bool> OnLightingSystemEnabled;

		public bool matrixRotationMode
		{
			get
			{
				return mMatrixRotationMode;
			}

			set
			{
				if (mMatrixRotationMode == value)
					return;

				mMatrixRotationMode = value;
			}
		}

		/// <summary>
		/// Holds the occlusion mask used by object sprites (such as wallmount and objects on the floor) to make them invisible when they are out of view. Not used for any other purpose.
		/// Generated from wallFloorOcclusionMask
		/// </summary>
		private PixelPerfectRT objectOcclusionMask
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

				Shader.SetGlobalTexture("_ObjectFovMask", value.renderTexture);
			}
		}

		/// <summary>
		/// Used to hold the UI layer
		/// </summary>
		private PixelPerfectRT UILayer
		{
			get
			{
				return mUILayer;
			}

			set
			{
				if (mUILayer == value)
					return;

				if (mUILayer != null)
				{
					mUILayer.Release();
				}

				mUILayer = value;
			}
		}

		/// <summary>
		/// Holds the occlusion mask which only masks out the floor based on the position of walls / the player. This ignores occluded walls and is used
		/// for the various lighting shaders (which look best when we use this rather than wallFloorOcclusionMask because that's what the lighting system
		/// was designed to use).
		/// </summary>
		private PixelPerfectRT floorOcclusionMask
		{
			get
			{
				return mFloorOcclusionMask;
			}

			set
			{
				if (mFloorOcclusionMask == value)
					return;

				if (mFloorOcclusionMask != null)
				{
					mFloorOcclusionMask.Release();
				}

				mFloorOcclusionMask = value;
			}
		}

		/// <summary>
		/// Holds the occlusion mask which occludes walls and floors, only used to generate objectOcclusionMask so that wallmounts
		/// are occluded along with objects on the floor which are out of view.
		/// </summary>
		private PixelPerfectRT wallFloorOcclusionMask
		{
			get
			{
				return mWallFloorOcclusionMask;
			}

			set
			{
				if (mWallFloorOcclusionMask == value)
					return;

				if (mWallFloorOcclusionMask != null)
				{
					mWallFloorOcclusionMask.Release();
				}

				mWallFloorOcclusionMask = value;
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

		private OperationParameters operationParameters
		{
			get
			{
				return mOperationParameters;
			}
			set
			{
				mOperationParameters = value;

				Shader.SetGlobalFloat("_LightingTilePixelsPerUnit", value.pixelsPerUnit);
			}
		}

		/// <summary>
		/// Transforms provided movement data to match pixel perfect space of lighting system.
		/// Used to clamp position of objects according to pixel perfect texture space, which is less detailed then world-space.
		/// Also filters out jitters that may occur when noisy position is clamped down to lower precision, which requires for client to store additional position data.
		/// </summary>
		/// <param name="iWorldSpacePosition">World space position to transform.</param>
		/// <param name="iPreviousWorldSpacePosition">Previous, untransformed world space position.</param>
		/// <param name="iPreviousPixelPerfectPosition">Previous, transformed world space position.</param>
		/// <returns>Transformed position that matches Pixel Perfect texture space.</returns>
		public static Vector2 GetPixelPerfectPosition(Vector3 iWorldSpacePosition, Vector3 iPreviousWorldSpacePosition, Vector2 iPreviousPixelPerfectPosition)
		{
			if (HandlePPPositionRequest == null)
			{
				return iWorldSpacePosition;
			}

			return HandlePPPositionRequest(iWorldSpacePosition, iPreviousWorldSpacePosition, iPreviousPixelPerfectPosition);
		}

		/// <summary>
		/// Checks if the specified screen position is occluded due to FOV.
		/// </summary>
		/// <param name="iScreenPoint">Screen-space position to test.</param>
		/// <returns>true iff the pixel at the specified screen position is not hidden by FOV occlusion. Note that a wallmount
		/// on the opposite side of the wall from a player is not occluded by FOV even though it is turned transparent. You can
		/// check the wallmount's sprite color alpha transparency to see if it is visible.</returns>
		public bool IsScreenPointVisible(Vector2 iScreenPoint)
		{
			//don't run lighting system on headless
			if (GameInfo.IsHeadlessServer)
			{
				return true;
			}
			Vector2 _viewportPos = mMainCamera.ScreenToViewportPoint(iScreenPoint);

			// Check for out of bounds;
			if (_viewportPos.x < 0.0f ||
			    _viewportPos.x > 1.0f ||
			    _viewportPos.y < 0.0f ||
			    _viewportPos.y > 1.0f)
				return false;

			// Use PPRT occlusion mask to get normalized texture coordinate.
			// There is an assumption that wallFloorOcclusionMask and requested AsyncGPUReadback texture is the same.
			Vector2 _normalizedMaskPoint = wallFloorOcclusionMask.ScreenToNormalTextureCoordinates(mMainCamera, iScreenPoint);

			Color32 _sampledColor;

			if (!renderSettings.disableAsyncGPUReadback && SystemInfo.supportsAsyncGPUReadback)
			{

				if (mTextureDataRequest.TryGetPixelNormalized(_normalizedMaskPoint.x, _normalizedMaskPoint.y,
					    out _sampledColor))
				{
					bool _sampledVisibleWall = _sampledColor.r > 0;
					bool _sampledVisibleFloor = _sampledColor.g > 0;

					// Consider visible wal or visible floor as visible point.
					return _sampledVisibleWall || _sampledVisibleFloor;
				}
			}
			else
			{
				Color color = mTex2DWallFloorOcclusionMask.GetPixelBilinear(_normalizedMaskPoint.x, _normalizedMaskPoint.y);
				return color == Color.red || color == Color.green;
			}

			return false;
		}

		private static void ValidateMainCamera(Camera iMainCamera, RenderSettings iRenderSettings)
		{
			if (iMainCamera.backgroundColor.a > 0)
			{
				Loggy.Info("FovSystem Camera Validation: Camera backgroundColor.a must be 0." +
				           " This is required to create background mask. Adjusted...", Category.Lighting);

				iMainCamera.backgroundColor = new Color(iMainCamera.backgroundColor.r, iMainCamera.backgroundColor.g, iMainCamera.backgroundColor.b, 0);
			}

			if (((LayerMask)iMainCamera.cullingMask).HasAny(iRenderSettings.lightSourceLayers))
			{
				Loggy.Info("FovSystem Camera Validation: Camera does not cull one of Light Source Layers!" +
				           " Light System may not work currently.", Category.Lighting);
			}
		}

		private void OnEnable()
		{
			Loggy.Info("Lighting system enabled.", Category.Lighting);
			//don't run lighting system on headless
			if (GameInfo.IsHeadlessServer)
			{
				return;
			}

			if (!SystemInfo.supportsAsyncGPUReadback)
			{
				Loggy.Warning("LightingSystem: Async GPU Readback not supported on this machine, slower synchronous readback will" +
				              " be used instead.", Category.Lighting);
			}
			HandlePPPositionRequest += ProviderPPPosition;

			// Initialize members.
			mMainCamera = gameObject.GetComponent<Camera>();

			if (mMainCamera == null)
				throw new Exception("FovSystemManager require Camera component to operate.");

			// Now validate other settings
			ValidateMainCamera(mMainCamera, renderSettings);

			mMainCamera.cullingMask = renderSettings.LightingEnabledMainCameraLayers;

			// The UI camera is rendered manually into a texture, so it must never render on its own.
			if (UICamera != null)
			{
				UICamera.enabled = false;
			}

			if (mOcclusionRenderer == null)
			{
				mOcclusionRenderer = OcclusionMaskRenderer.InitializeMaskRenderer(gameObject, renderSettings.occlusionLayers);
			}

			mShadowMaskRenderer.SetUp(gameObject);
			FulbrightRenderer.SetUp(gameObject);

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

			if (!renderSettings.disableAsyncGPUReadback && SystemInfo.supportsAsyncGPUReadback)
			{
				mTextureDataRequest = new TextureDataRequest();
			}

			operationParameters = new OperationParameters(mMainCamera, renderSettings, matrixRotationMode);

			OnLightingSystemEnabled?.Invoke(true);

			ResolveRenderingTextures(operationParameters);
		}

		private Vector2 ProviderPPPosition(Vector3 iPosition, Vector3 iPreviousPosition, Vector2 iPreviousFilteredPosition)
		{
			return operationParameters.occlusionPPRTParameter.GetFilteredRendererPosition(iPosition, iPreviousPosition, iPreviousFilteredPosition);
		}

		private void OnDisable()
		{
			Loggy.Info("Lighting system disabled.", Category.Lighting);
			//don't run lighting system on headless
			if (GameInfo.IsHeadlessServer)
			{
				return;
			}

			compositeMode = CompositeMode.None;

			OnLightingSystemEnabled?.Invoke(false);
			mMainCamera.cullingMask = renderSettings.LightingDisabledMainCameraLayers;
			// Set object occlusion white, so occlusion dependent shaders will show appropriately while system is off.
			Shader.SetGlobalTexture("_ObjectFovMask", Texture2D.whiteTexture);

			// Default parameters to force parameters update on enable.
			operationParameters = default(OperationParameters);

			HandlePPPositionRequest -= ProviderPPPosition;

			if (mTextureDataRequest != null)
			{
				mTextureDataRequest.Dispose();
				mTextureDataRequest = null;
			}
		}

		private void OnDestroy()
		{
			ReleaseScreenTargets();
		}

		public void LateUpdate()
		{
			// Don't run lighting system on headless.
			if (GameInfo.IsHeadlessServer)
			{
				return;
			}

			// Monitor state to detect when we should trigger reinitialization of rendering textures.
			var _newParameters = new OperationParameters(mMainCamera, renderSettings, matrixRotationMode);

			bool _shouldReinitializeTextures = _newParameters != operationParameters;
			if (_shouldReinitializeTextures)
			{
				operationParameters = _newParameters;

				ResolveRenderingTextures(operationParameters);
			}

			// Blend switch for matrix rotation effects.
			// Used to smooth effects in and out.
			if (mMatrixRotationMode == true)
			{
				mMatrixRotationModeBlend = Mathf.MoveTowards(mMatrixRotationModeBlend, 1, Time.unscaledDeltaTime * 5);
			}
			else
			{
				mMatrixRotationModeBlend = 0;
			}

			compositeMode = CompositeMode.None;
			compositeUI = false;

			RunMaskPhase();   // formerly OnPreRender
			RunLightPhase();  // formerly OnRenderImage (everything except the final blits)
		}

		/// <summary>
		/// Renders a camera into a texture via the SRP render request API.
		/// Must be called outside the render loop (e.g. from LateUpdate).
		/// </summary>
		public static void RenderToTexture(Camera cam, RenderTexture rt)
		{
			var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };

			if (RenderPipeline.SupportsRenderRequest(cam, request))
			{
				RenderPipeline.SubmitRenderRequest(cam, request);
			}
			else
			{
				Loggy.Warning($"LightingSystem: camera '{cam.name}' does not support render requests (is it an Overlay camera?).", Category.Lighting);
			}
		}

		private void ResolveRenderingTextures(OperationParameters iParameters)
		{
			// Prepare render textures.
			floorOcclusionMask = new PixelPerfectRT(operationParameters.fovPPRTParameter);
			wallFloorOcclusionMask = new PixelPerfectRT(operationParameters.fovPPRTParameter);

			if (mTex2DWallFloorOcclusionMask != null)
			{
				Destroy(mTex2DWallFloorOcclusionMask);
			}
			mTex2DWallFloorOcclusionMask = new Texture2D(wallFloorOcclusionMask.renderTexture.width, wallFloorOcclusionMask.renderTexture.height, TextureFormat.RGB24, false);

			UILayer = new PixelPerfectRT(operationParameters.UICameraPPRTParameter);

			objectOcclusionMask = new PixelPerfectRT(operationParameters.lightPPRTParameter);

			obstacleLightMask = new PixelPerfectRT(operationParameters.obstacleLightPPRTParameter);

			// Let members handle their own textures.
			// Possibly move to container?
			mPostProcessingStack.ResetRenderingTextures(iParameters);
			mBackgroundRenderer.ResetRenderingTextures(iParameters);
			mShadowMaskRenderer.ResetRenderingTextures(iParameters);
			FulbrightRenderer.ResetRenderingTextures(iParameters);
			UICamera.orthographicSize = iParameters.cameraOrthographicSize;
			mMainCamera.orthographicSize = iParameters.cameraOrthographicSize;
		}

		// Phase 1: occlusion + FoV masks (was OnPreRender)
		private void RunMaskPhase()
		{
			if (renderSettings.doubleFrameRenderingMode && mDoubleFrameRendererSwitch == false)
			{
				Shader.SetGlobalVector("_ObjectFovMaskTransformation", objectOcclusionMask.GetTransformation(mMainCamera));
				return;
			}
			if (!Application.isPlaying)
			{
				return;
			}

			using (new DisposableProfiler("1. Occlusion Mask Render (No Gfx Time)"))
			{
				mOcclusionPPRT = mOcclusionRenderer.Render(mMainCamera, operationParameters.occlusionPPRTParameter, matrixRotationMode);

				if (mMatrixRotationModeBlend > 0.001f)
				{
					mPostProcessingStack.BlurOcclusionMaskRotation(mOcclusionPPRT.renderTexture, renderSettings, operationParameters.cameraOrthographicSize, mMatrixRotationModeBlend);
				}
			}

			using (new DisposableProfiler("2. Generate FoV"))
			{
				if (wallFloorOcclusionMask == null)
				{
					floorOcclusionMask = new PixelPerfectRT(operationParameters.fovPPRTParameter);
					wallFloorOcclusionMask = new PixelPerfectRT(operationParameters.fovPPRTParameter);
				}
				else
				{
					floorOcclusionMask.Update(operationParameters.fovPPRTParameter);
					wallFloorOcclusionMask.Update(operationParameters.fovPPRTParameter);
				}

				// This step will result in two masks: floorOcclusionMask used later in light mixing, and floorWallOcclusionMask which is only used for calculating
				// objects / wallmounts sprite visibility.
				Vector3 _fovCenterInWorldSpace = transform.TransformPoint(fovCenterOffset);
				Vector3 _fovCenterOffsetInViewSpace = mMainCamera.WorldToViewportPoint(_fovCenterInWorldSpace) - new Vector3(0.5f, 0.5f, 0);
				Vector3 _fovCenterOffsetInExtendedViewSpace = _fovCenterOffsetInViewSpace * (float)operationParameters.cameraOrthographicSize / mOcclusionPPRT.orthographicSize;

				mPostProcessingStack.GenerateFovMask(mOcclusionPPRT,
					floorOcclusionMask,
					wallFloorOcclusionMask,
					renderSettings,
					_fovCenterOffsetInExtendedViewSpace,
					fovDistance,
					operationParameters);

				if (!renderSettings.disableAsyncGPUReadback && SystemInfo.supportsAsyncGPUReadback)
				{
					// Request asynchronous callback for access to texture data.
					// Used for sampling point visibility from mask.
					AsyncGPUReadback.Request(wallFloorOcclusionMask.renderTexture, 0, AsyncReadCallback);
				}
				else
				{
					//async readback not supported, instead use synchronous readback
					RenderTexture.active = wallFloorOcclusionMask.renderTexture;
					mTex2DWallFloorOcclusionMask.ReadPixels(new Rect(0, 0, wallFloorOcclusionMask.renderTexture.width, wallFloorOcclusionMask.renderTexture.height), 0, 0);
					mTex2DWallFloorOcclusionMask.Apply();
					RenderTexture.active = null;
				}
			}

			using (new DisposableProfiler("3. Object Occlusion Mask"))
			{
				// These step calculates the objectOcclusionMask using the wallFloorOcclusionMask, so that objects / wallmounts will be hidden
				// if they are not in view
				objectOcclusionMask.Update(operationParameters.lightPPRTParameter);
				PixelPerfectRT.Transform(wallFloorOcclusionMask, objectOcclusionMask, materialContainer);

				// Update shader global transformation for occlusionMaskExtended so sprites can transform mask correctly.
				Shader.SetGlobalVector("_ObjectFovMaskTransformation", objectOcclusionMask.GetTransformation(mMainCamera));
			}

			using (new DisposableProfiler("4. Blur Object Occlusion Mask"))
			{
				// Note: This blur is used only with shaders during scene render, so 1 pass should be enough.
				mPostProcessingStack.BlurOcclusionMask(objectOcclusionMask.renderTexture, renderSettings, operationParameters.cameraOrthographicSize);

				SetFilterMode(objectOcclusionMask.renderTexture, FilterMode.Point);
			}
		}

		/// <summary>
		/// Handle data access callback from gpu.
		/// Wraps data request in to struct that handles protection and sampling.
		/// </summary>
		/// <param name="iRequest">requested callback to wrap.</param>
		private void AsyncReadCallback(AsyncGPUReadbackRequest iRequest)
		{
			if (iRequest.hasError || iRequest.done == false || mTextureDataRequest == null)
			{
				return;
			}

			mTextureDataRequest.StoreData(iRequest);
		}

		// Phase 2: light / shadow / background / UI textures + composite setup
		// The final blit of the camera color is done by LightingRendererFeature.
		private void RunLightPhase()
		{
			var _blitMaterial = materialContainer.blitMaterial;

			if (renderSettings.doubleFrameRenderingMode)
			{
				if (mDoubleFrameRendererSwitch)
				{
					// Second frame: reuse last frame's textures, only update transforms.
					if (_blitMaterial != null && mlightPPRT != null)
					{
						_blitMaterial.SetVector("_LightTransform", mlightPPRT.GetTransformation(mMainCamera));
						_blitMaterial.SetVector("_OcclusionTransform", floorOcclusionMask.GetTransformation(mMainCamera));
						compositeMode = CompositeMode.Lit;
						compositeUI = false;
					}

					mDoubleFrameRendererSwitch = false;
					return;
				}

				mDoubleFrameRendererSwitch = true;
			}

			if (_blitMaterial == null)
			{
				Loggy.Info().Format("FovSystemManager: Unable to blit Fov mask. {0} not provided.", Category.Lighting, nameof(materialContainer.blitMaterial));
				return;
			}

			if (objectOcclusionMask == null)
			{
				return; // CompositeMode.None: camera output is left as rendered.
			}

			using (new DisposableProfiler("5. Light Mask Render (No Gfx Time)"))
			{
				mlightPPRT = mLightMaskRenderer.Render(
					mMainCamera,
					operationParameters.lightPPRTParameter,
					floorOcclusionMask,
					renderSettings,
					matrixRotationMode);
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
			switch (renderSettings.viewMode)
			{
				case RenderSettings.ViewMode.LightLayer:
					ShowDebugView(mlightPPRT);
					return;
				case RenderSettings.ViewMode.WallLayer:
					ShowDebugView(obstacleLightMask);
					return;
				case RenderSettings.ViewMode.FovObjectOcclusion:
					ShowDebugView(objectOcclusionMask);
					return;
				case RenderSettings.ViewMode.FovFloorOcclusion:
					ShowDebugView(floorOcclusionMask);
					return;
				case RenderSettings.ViewMode.FovWallAndFloorOcclusion:
					ShowDebugView(wallFloorOcclusionMask);
					return;
				case RenderSettings.ViewMode.Obstacle:
					ShowDebugView(mOcclusionPPRT);
					return;
			}

			using (new DisposableProfiler("7. Light Mask Blur"))
			{
				mPostProcessingStack.BlurLightMask(mlightPPRT.renderTexture, renderSettings, operationParameters.cameraOrthographicSize, mMatrixRotationModeBlend);
			}

			RenderTexture _backgroundMask = null;

			using (new DisposableProfiler("8. Render Background"))
			{
				_backgroundMask = mBackgroundRenderer.Render(renderSettings);
			}

			// Debug Views Selection.
			if (renderSettings.viewMode == RenderSettings.ViewMode.LightLayerBlurred)
			{
				ShowDebugView(mlightPPRT);
				return;
			}
			else if (renderSettings.viewMode == RenderSettings.ViewMode.Background)
			{
				EnsureScreenTargets();
				Graphics.Blit(_backgroundMask, mDebugRenderTexture);
				compositeMode = CompositeMode.Debug;
				return;
			}

			using (new DisposableProfiler("9. Shadow Silhouette + Classify + Blur"))
			{
				mShadowMaskRenderer.Render(mMainCamera);
			}

			using (new DisposableProfiler("9.5. Render the Fulbright But affected by FOV items"))
			{
				FulbrightRenderer.Render(mMainCamera);
			}

			using (new DisposableProfiler("10. Configure Mixed Lights Blit"))
			{
				SetFilterMode(mlightPPRT.renderTexture, FilterMode.Bilinear);
				SetFilterMode(obstacleLightMask.renderTexture, FilterMode.Bilinear);
				SetFilterMode(floorOcclusionMask.renderTexture, matrixRotationMode ? FilterMode.Bilinear : FilterMode.Point);

				_blitMaterial.SetTexture("_LightMask", mlightPPRT.renderTexture);
				_blitMaterial.SetTexture("_OcclusionMask", floorOcclusionMask.renderTexture);
				_blitMaterial.SetTexture("_ObstacleLightMask", obstacleLightMask.renderTexture);
				_blitMaterial.SetVector("_LightTransform", mlightPPRT.GetTransformation(mMainCamera));
				_blitMaterial.SetVector("_OcclusionTransform", floorOcclusionMask.GetTransformation(mMainCamera));

				_blitMaterial.SetTexture("_BackgroundTex", _backgroundMask);
				_blitMaterial.SetVector("_AmbLightBloomSA", new Vector4(renderSettings.ambient, renderSettings.lightMultiplier, renderSettings.bloomSensitivity, renderSettings.bloomAdd));
				_blitMaterial.SetFloat("_BackgroundMultiplier", renderSettings.backgroundMultiplier);

				_blitMaterial.SetTexture("_FullbrightTex", FulbrightRenderer._FulbrightRT);
				_blitMaterial.SetTexture("_ItemTex", mShadowMaskRenderer._ItemRT);
				_blitMaterial.SetTexture("_ShadowTex", mShadowMaskRenderer._pongRT);
				_blitMaterial.SetFloat("_ShadowAlpha", ShadowMaskRenderer.shadowAlpha);

				compositeMode = CompositeMode.Lit;
			}

			using (new DisposableProfiler("11. Render UI Layer"))
			{
				if (DoUICamera && UICamera != null)
				{
					EnsureScreenTargets();

					// UI camera renders into a persistent texture (replaces GetTemporary + Camera.Render).
					RenderToTexture(UICamera, mUIRenderTexture);
				}
			}

			if (DoOtherBit && uiCompositeMaterial != null && mUIRenderTexture != null)
			{
				uiCompositeMaterial.SetTexture("_UITexture", mUIRenderTexture);
				compositeUI = true;
			}
		}

		private void ShowDebugView(PixelPerfectRT iSource)
		{
			EnsureScreenTargets();
			PixelPerfectRT.Transform(iSource, mDebugRenderTexture, mMainCamera, materialContainer);
			compositeMode = CompositeMode.Debug;
		}

		/// <summary>
		/// (Re)creates the screen sized UI and debug targets when the camera size changes.
		/// Both have a depth buffer, which URP's Render Graph requires on camera output textures.
		/// </summary>
		private void EnsureScreenTargets()
		{
			int _w = Mathf.Max(1, mMainCamera.pixelWidth);
			int _h = Mathf.Max(1, mMainCamera.pixelHeight);

			if (mUIRenderTexture == null || mUIRenderTexture.width != _w || mUIRenderTexture.height != _h)
			{
				if (mUIRenderTexture != null)
				{
					mUIRenderTexture.Release();
					Destroy(mUIRenderTexture);
				}

				mUIRenderTexture = new RenderTexture(_w, _h, 24, RenderTextureFormat.ARGB32)
				{
					name = "LightingSystem UI"
				};
			}

			if (mDebugRenderTexture == null || mDebugRenderTexture.width != _w || mDebugRenderTexture.height != _h)
			{
				mDebugHandle?.Release();

				if (mDebugRenderTexture != null)
				{
					mDebugRenderTexture.Release();
					Destroy(mDebugRenderTexture);
				}

				mDebugRenderTexture = new RenderTexture(_w, _h, 0, RenderTextureFormat.ARGB32)
				{
					name = "LightingSystem Debug"
				};
				mDebugHandle = RTHandles.Alloc(mDebugRenderTexture);
			}
		}

		private void ReleaseScreenTargets()
		{
			mDebugHandle?.Release();
			mDebugHandle = null;

			if (mDebugRenderTexture != null)
			{
				mDebugRenderTexture.Release();
				Destroy(mDebugRenderTexture);
				mDebugRenderTexture = null;
			}

			if (mUIRenderTexture != null)
			{
				mUIRenderTexture.Release();
				Destroy(mUIRenderTexture);
				mUIRenderTexture = null;
			}
		}

		private static void SetFilterMode(RenderTexture iTexture, FilterMode iMode)
		{
			if (iTexture != null && iTexture.filterMode != iMode)
			{
				iTexture.filterMode = iMode;
			}
		}
	}
}