using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Light2D
{
	/// <summary>
	///     Main script for lights. Should be attached to camera.
	///     Handles lighting operation like camera setup, shader setup, merging cameras output together, blurring and some
	///     others.
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public class LightingSystem : MonoBehaviour
	{
		/// <summary>
		///     Size of lighting pixel in Unity meters. Controls resoultion of lighting textures.
		///     Smaller value - better quality, but lower performance.
		/// </summary>
		public float LightPixelSize = 0.05f;

		/// <summary>
		///     Needed for off screen lights to work correctly. Set that value to radius of largest light.
		///     Used only when camera is in orthographic mode. Big values could cause a performance drop.
		/// </summary>
		public float LightCameraSizeAdd = 3;

		/// <summary>
		///     Needed for off screen lights to work correctly.
		///     Used only when camera is in perspective mode.
		/// </summary>
		public float LightCameraFovAdd = 30;

		/// <summary>
		///     Enable/disable ambient lights. Disable it to improve performance if you not using ambient light.
		/// </summary>
		public bool EnableAmbientLight = true;

		/// <summary>
		///     LightSourcesBlurMaterial is applied to light sources texture if enabled. Disable to improve performance.
		/// </summary>
		public bool BlurLightSources = true;

		/// <summary>
		///     AmbientLightBlurMaterial is applied to ambient light texture if enabled. Disable to improve performance.
		/// </summary>
		public bool BlurAmbientLight = true;

		/// <summary>
		///     If true RGBHalf RenderTexture type will be used for light processing.
		///     That could improve smoothness of lights. Will be turned off if device is not supports it.
		/// </summary>
		public bool HDR = true;

		/// <summary>
		///     If true light obstacles will be rendered in 2x resolution and then downsampled to 1x.
		/// </summary>
		public bool LightObstaclesAntialiasing = true;

		/// <summary>
		///     Set it to distance from camera to plane with light obstacles. Used only when camera in perspective mode.
		/// </summary>
		public float LightObstaclesDistance = 10;

		/// <summary>
		///     Billinear for blurred lights, Point for pixelated lights.
		/// </summary>
		public FilterMode LightTexturesFilterMode = FilterMode.Bilinear;

		/// <summary>
		///     Normal mapping. Not supported on mobiles.
		/// </summary>
		public bool EnableNormalMapping;

		/// <summary>
		///     If true lighting won't be seen on contents of previous cameras.
		/// </summary>
		public bool AffectOnlyThisCamera;

		public Material AmbientLightComputeMaterial;
		public Material LightOverlayMaterial;
		public Material LightSourcesBlurMaterial;
		public Material AmbientLightBlurMaterial;
		public Camera LightCamera;
		public Camera BgCamera;
		public int LightSourcesLayer;
		public int AmbientLightLayer;
		public int LightObstaclesLayer;
		public bool XZPlane;

		private RenderTexture _ambientEmissionTexture;
		private RenderTexture _ambientTexture;
		private RenderTexture _prevAmbientTexture;
		private RenderTexture _bluredLightTexture;
		private RenderTexture _obstaclesUpsampledTexture;
		private RenderTexture _lightSourcesTexture;
		private RenderTexture _obstaclesTexture;
		private RenderTexture _screenBlitTempTex;
		private RenderTexture _normalMapBuffer;
		private RenderTexture _singleLightSourceTexture;
		public RenderTexture _renderTargetTexture { get; set; }
		private RenderTexture _oldActiveRenderTexture;

		public Camera _camera { get; set; }
		private ObstacleCameraPostPorcessor _obstaclesPostProcessor;
		private Point2 _extendedLightTextureSize;
		private Point2 _smallLightTextureSize;
		private Vector3 _oldPos;
		private Vector3 _currPos;
		private RenderTextureFormat _texFormat;
		private int _aditionalAmbientLightCycles;
		private static LightingSystem _instance;
		private Shader _normalMapRenderShader;
		private Camera _normalMapCamera;
		private readonly List<LightSprite> _lightSpritesCache = new List<LightSprite>();
		private Material _normalMappedLightMaterial;
		private Material _lightCombiningMaterial;
		private Material _alphaBlendedMaterial;
		private bool _halfTexelOffest;
#if LIGHT2D_2DTK
        private tk2dCamera _tk2dCamera;
#endif

		private float LightPixelsPerUnityMeter => 1 / LightPixelSize;

		public static LightingSystem Instance => _instance != null ? _instance : (_instance = FindObjectOfType<LightingSystem>());


		private void OnEnable()
		{
			_instance = this;
			_camera = GetComponent<Camera>();
		}

		private void Start()
		{
			if (GameData.Instance.testServer || GameData.IsHeadlessServer)
			{
				Debug.Log("Turn off lightsystem as this is a server");
				enabled = false;
			}
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				Shader.SetGlobalTexture("_ObstacleTex", Texture2D.whiteTexture);
				return;
			}
#endif

			if (LightCamera == null)
			{
				Debug.LogError("Lighting Camera in LightingSystem is null. Please, select Lighting Camera camera for lighting to work.");
				enabled = false;
				return;
			}
			if (LightOverlayMaterial == null)
			{
				Debug.LogError("LightOverlayMaterial in LightingSystem is null. Please, select LightOverlayMaterial camera for lighting to work.");
				enabled = false;
				return;
			}
			if (AffectOnlyThisCamera && _camera.targetTexture != null)
			{
				Debug.LogError("\"Affect Only This Camera\" will not work if camera.targetTexture is set.");
				AffectOnlyThisCamera = false;
			}

			_camera = GetComponent<Camera>();

			if (EnableNormalMapping && !_camera.orthographic)
			{
				Debug.LogError("Normal mapping is not supported with perspective camera.");
				EnableNormalMapping = false;
			}

			// if both FlareLayer component and AffectOnlyThisCamera setting is enabled
			// Unity will print an error "Flare renderer to update not found" 
			FlareLayer flare = GetComponent<FlareLayer>();
			if (flare != null && flare.enabled)
			{
				Debug.Log("Disabling FlareLayer since AffectOnlyThisCamera setting is checked.");
				flare.enabled = false;
			}

			if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
			{
				HDR = false;
			}
			_texFormat = HDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

			float lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;

			_halfTexelOffest = SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 9");

			InitTK2D();

			if (_camera.orthographic)
			{
				float rawCamHeight = (_camera.orthographicSize + LightCameraSizeAdd) * 2f;
				float rawCamWidth = (_camera.orthographicSize * _camera.aspect + LightCameraSizeAdd) * 2f;

				_extendedLightTextureSize = new Point2(Mathf.RoundToInt(rawCamWidth * lightPixelsPerUnityMeter),
					Mathf.RoundToInt(rawCamHeight * lightPixelsPerUnityMeter));

				float rawSmallCamHeight = _camera.orthographicSize * 2f * lightPixelsPerUnityMeter;
				_smallLightTextureSize = new Point2(Mathf.RoundToInt(rawSmallCamHeight * _camera.aspect), Mathf.RoundToInt(rawSmallCamHeight));
			}
			else
			{
				{
					float lightCamHalfFov = (_camera.fieldOfView + LightCameraFovAdd) * Mathf.Deg2Rad / 2f;
					float lightCamSize = Mathf.Tan(lightCamHalfFov) * LightObstaclesDistance * 2;
					//var gameCamHalfFov = _camera.fieldOfView*Mathf.Deg2Rad/2f;
					int texHeight = Mathf.RoundToInt(lightCamSize / LightPixelSize);
					float texWidth = texHeight * _camera.aspect;
					_extendedLightTextureSize = Point2.Round(new Vector2(texWidth, texHeight));
				}
				{
					float lightCamHalfFov = _camera.fieldOfView * Mathf.Deg2Rad / 2f;
					float lightCamSize = Mathf.Tan(lightCamHalfFov) * LightObstaclesDistance * 2;
					//LightCamera.orthographicSize = lightCamSize/2f;

					float gameCamHalfFov = _camera.fieldOfView * Mathf.Deg2Rad / 2f;
					float gameCamSize = Mathf.Tan(gameCamHalfFov) * LightObstaclesDistance * 2;
					_camera.orthographicSize = gameCamSize / 2f;

					int texHeight = Mathf.RoundToInt(lightCamSize / LightPixelSize);
					float texWidth = texHeight * _camera.aspect;
					_smallLightTextureSize = Point2.Round(new Vector2(texWidth, texHeight));
				}
			}

			if (_extendedLightTextureSize.x % 2 != 0)
			{
				_extendedLightTextureSize.x++;
			}
			if (_extendedLightTextureSize.y % 2 != 0)
			{
				_extendedLightTextureSize.y++;
			}

			if (_extendedLightTextureSize.x > 1024 || _extendedLightTextureSize.y > 1024 || _smallLightTextureSize.x > 1024 || _smallLightTextureSize.y > 1024)
			{
				Debug.LogError("LightPixelSize is too small. That might have a performance impact.");
				return;
			}

			if (_extendedLightTextureSize.x < 4 || _extendedLightTextureSize.y < 4 || _smallLightTextureSize.x < 4 || _smallLightTextureSize.y < 4)
			{
				Debug.LogError("LightPixelSize is too big. Lighting may not work correctly.");
				return;
			}

			_screenBlitTempTex = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, _texFormat);
			_screenBlitTempTex.filterMode = FilterMode.Point;

			LightCamera.orthographic = _camera.orthographic;

			if (EnableNormalMapping)
			{
				_lightSourcesTexture = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, _texFormat);
				_lightSourcesTexture.filterMode = FilterMode.Point;
			}
			else
			{
				_lightSourcesTexture = new RenderTexture(_smallLightTextureSize.x, _smallLightTextureSize.y, 0, _texFormat);
				_lightSourcesTexture.filterMode = LightTexturesFilterMode;
			}

			_obstaclesTexture = new RenderTexture(_extendedLightTextureSize.x, _extendedLightTextureSize.y, 0, _texFormat);
			_ambientTexture = new RenderTexture(_extendedLightTextureSize.x, _extendedLightTextureSize.y, 0, _texFormat);

			_ambientTexture.filterMode = LightTexturesFilterMode;

			Point2 upsampledObstacleSize = _extendedLightTextureSize * (LightObstaclesAntialiasing ? 2 : 1);
			_obstaclesUpsampledTexture = new RenderTexture(upsampledObstacleSize.x, upsampledObstacleSize.y, 0, _texFormat);

			if (AffectOnlyThisCamera)
			{
				_renderTargetTexture = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, -2, RenderTextureFormat.ARGB32);
				_renderTargetTexture.filterMode = FilterMode.Point;
				_camera.targetTexture = _renderTargetTexture;
				_camera.clearFlags = CameraClearFlags.SolidColor;
				_camera.backgroundColor = Color.clear;
			}

			_alphaBlendedMaterial = new Material(Shader.Find("Light2D/Internal/Alpha Blended"));

			if (XZPlane)
			{
				Shader.EnableKeyword("LIGHT2D_XZ_PLANE");
			}
			else
			{
				Shader.DisableKeyword("LIGHT2D_XZ_PLANE");
			}

			_obstaclesPostProcessor = new ObstacleCameraPostPorcessor();

			LoopAmbientLight(100);
		}

		private void OnRenderImage(RenderTexture src, RenderTexture dest)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying || Util.IsSceneViewFocused)
			{
				Shader.SetGlobalTexture("_ObstacleTex", Texture2D.whiteTexture);
				if (dest != null)
				{
					dest.DiscardContents();
				}
				Graphics.Blit(src, dest);
				return;
			}
#endif
			Update2DTK();
			UpdateCamera();
			RenderObstacles();
			SetupShaders();
			RenderNormalBuffer();
			RenderLightSources();
			RenderLightSourcesBlur();
			RenderAmbientLight();

			RenderLightOverlay(src, dest);
		}

		private void OnPreRender()
		{
			if (BgCamera != null)
			{
				BgCamera.Render();
			}
		}

		private void OnPreCull()
		{
			if (Application.isPlaying && AffectOnlyThisCamera)
			{
				_camera.targetTexture = _renderTargetTexture;
			}
		}

		private void OnRenderObject()
		{
			if (Application.isPlaying && AffectOnlyThisCamera)
			{
				_camera.targetTexture = null;
				Graphics.Blit(_renderTargetTexture, null, _alphaBlendedMaterial);
				_camera.targetTexture = _renderTargetTexture;
			}
		}

		private void InitTK2D()
		{
#if LIGHT2D_2DTK
            _tk2dCamera = GetComponent<tk2dCamera>();
            if (_tk2dCamera != null && _tk2dCamera.CameraSettings.projection == tk2dCameraSettings.ProjectionType.Orthographic)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = _tk2dCamera.ScreenExtents.yMax;
            }
#endif
		}

		private void Update2DTK()
		{
#if LIGHT2D_2DTK
            if (_tk2dCamera != null && _tk2dCamera.CameraSettings.projection == tk2dCameraSettings.ProjectionType.Orthographic)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = _tk2dCamera.ScreenExtents.yMax;
            }
#endif
		}

		private void LateUpdate()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying && LightCamera != null)
			{
				_camera = GetComponent<Camera>();
				if (_camera != null)
				{
					InitTK2D();
					LightCamera.orthographic = _camera.orthographic;
					if (_camera.orthographic)
					{
						LightCamera.orthographicSize = _camera.orthographicSize + LightCameraSizeAdd;
					}
					else
					{
						LightCamera.fieldOfView = _camera.fieldOfView + LightCameraFovAdd;
					}
				}
			}
			if (!Application.isPlaying || Util.IsSceneViewFocused)
			{
				Shader.SetGlobalTexture("_ObstacleTex", Texture2D.whiteTexture);
			}
#endif
		}

		private void RenderObstacles()
		{
			ConfigLightCamera(true);

			Color oldColor = LightCamera.backgroundColor;
			LightCamera.enabled = false;
			LightCamera.targetTexture = _obstaclesUpsampledTexture;
			LightCamera.cullingMask = 1 << LightObstaclesLayer;
			LightCamera.backgroundColor = new Color(1, 1, 1, 0);

			_obstaclesPostProcessor.DrawMesh(LightCamera, LightObstaclesAntialiasing ? 2 : 1);

			LightCamera.Render();
			LightCamera.targetTexture = null;
			LightCamera.cullingMask = 0;
			LightCamera.backgroundColor = oldColor;

			_obstaclesTexture.DiscardContents();
			Graphics.Blit(_obstaclesUpsampledTexture, _obstaclesTexture);
		}

		private void SetupShaders()
		{
			float lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;

			if (HDR)
			{
				Shader.EnableKeyword("HDR");
			}
			else
			{
				Shader.DisableKeyword("HDR");
			}

			if (_camera.orthographic)
			{
				Shader.DisableKeyword("PERSPECTIVE_CAMERA");
			}
			else
			{
				Shader.EnableKeyword("PERSPECTIVE_CAMERA");
			}

			Shader.SetGlobalTexture("_ObstacleTex", _obstaclesTexture);
			Shader.SetGlobalFloat("_PixelsPerBlock", lightPixelsPerUnityMeter);
			Shader.SetGlobalVector("_ExtendedToSmallTextureScale",
				new Vector2(_smallLightTextureSize.x / (float) _extendedLightTextureSize.x, _smallLightTextureSize.y / (float) _extendedLightTextureSize.y));
			Shader.SetGlobalVector("_PosOffset",
				LightObstaclesAntialiasing
					? (EnableNormalMapping ? _obstaclesUpsampledTexture.texelSize * 0.75f : _obstaclesUpsampledTexture.texelSize * 0.25f)
					: (EnableNormalMapping ? _obstaclesTexture.texelSize : _obstaclesTexture.texelSize * 0.5f));
		}

		private void RenderNormalBuffer()
		{
			if (!EnableNormalMapping)
			{
				return;
			}

			if (_normalMapBuffer == null)
			{
				_normalMapBuffer = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0, RenderTextureFormat.ARGB32);
				_normalMapBuffer.filterMode = FilterMode.Point;
			}

			if (_normalMapRenderShader == null)
			{
				_normalMapRenderShader = Shader.Find("Light2D/Internal/Normal Map Drawer");
			}

			if (_normalMapCamera == null)
			{
				GameObject camObj = new GameObject();
				camObj.name = "Normals Camera";
				camObj.transform.parent = _camera.transform;
				camObj.transform.localScale = Vector3.one;
				camObj.transform.localPosition = Vector3.zero;
				camObj.transform.localRotation = Quaternion.identity;
				_normalMapCamera = camObj.AddComponent<Camera>();
				_normalMapCamera.enabled = false;
			}

			_normalMapBuffer.DiscardContents();
			_normalMapCamera.CopyFrom(_camera);
			_normalMapCamera.transform.position = LightCamera.transform.position;
			_normalMapCamera.clearFlags = CameraClearFlags.SolidColor;
			_normalMapCamera.targetTexture = _normalMapBuffer;
			_normalMapCamera.cullingMask = int.MaxValue;
			_normalMapCamera.backgroundColor = new Color(0.5f, 0.5f, 0, 1);
			_normalMapCamera.RenderWithShader(_normalMapRenderShader, "LightObstacle");

			Shader.SetGlobalTexture("_NormalsBuffer", _normalMapBuffer);
			Shader.EnableKeyword("NORMAL_MAPPED_LIGHTS");
		}

		private void RenderLightSources()
		{
			ConfigLightCamera(false);

			if (EnableNormalMapping)
			{
				if (_singleLightSourceTexture == null)
				{
					_singleLightSourceTexture = new RenderTexture(_smallLightTextureSize.x, _smallLightTextureSize.y, 0, _texFormat);
					_singleLightSourceTexture.filterMode = LightTexturesFilterMode;
				}

				if (_normalMappedLightMaterial == null)
				{
					_normalMappedLightMaterial = new Material(Shader.Find("Light2D/Internal/Normal Mapped Light"));
					_normalMappedLightMaterial.SetTexture("_MainTex", _singleLightSourceTexture);
				}

				if (_lightCombiningMaterial == null)
				{
					_lightCombiningMaterial = new Material(Shader.Find("Light2D/Internal/Light Blender"));
					_lightCombiningMaterial.SetTexture("_MainTex", _singleLightSourceTexture);
				}

				Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
				_lightSourcesTexture.DiscardContents();

				Color oldBackgroundColor = LightCamera.backgroundColor;
				RenderTexture oldRt = RenderTexture.active;
				Graphics.SetRenderTarget(_lightSourcesTexture);
				GL.Clear(false, true, oldBackgroundColor);
				Graphics.SetRenderTarget(oldRt);

				_lightSpritesCache.Clear();
				foreach (LightSprite lightSprite in LightSprite.AllLightSprites)
				{
					if (lightSprite.RendererEnabled && GeometryUtility.TestPlanesAABB(cameraPlanes, lightSprite.Renderer.bounds))
					{
						_lightSpritesCache.Add(lightSprite);
					}
				}

				Vector3 lightCamLocPos = LightCamera.transform.localPosition;
				LightCamera.targetTexture = _singleLightSourceTexture;
				LightCamera.cullingMask = 0;
				LightCamera.backgroundColor = new Color(0, 0, 0, 0);

				foreach (LightSprite lightSprite in _lightSpritesCache)
				{
					// HACK: won't work for unknown reason without that line
					LightCamera.RenderWithShader(_normalMapRenderShader, "f84j");

					Graphics.SetRenderTarget(_singleLightSourceTexture);
					lightSprite.DrawLightingNow(lightCamLocPos);
					Graphics.SetRenderTarget(_lightSourcesTexture);
					lightSprite.DrawLightNormalsNow(_normalMappedLightMaterial);
				}
				Graphics.SetRenderTarget(oldRt);

				LightCamera.cullingMask = 1 << LightSourcesLayer;
				LightCamera.Render();
				Graphics.Blit(_singleLightSourceTexture, _lightSourcesTexture, _lightCombiningMaterial);

				LightCamera.targetTexture = null;
				LightCamera.cullingMask = 0;
				LightCamera.backgroundColor = oldBackgroundColor;
			}
			else
			{
				LightCamera.targetTexture = _lightSourcesTexture;
				LightCamera.cullingMask = 1 << LightSourcesLayer;
				//LightCamera.backgroundColor = new Color(0, 0, 0, 0);
				LightCamera.Render();
				LightCamera.targetTexture = null;
				LightCamera.cullingMask = 0;
			}
		}

		private void RenderLightSourcesBlur()
		{
			if (BlurLightSources && LightSourcesBlurMaterial != null)
			{
				Profiler.BeginSample("LightingSystem.OnRenderImage Bluring Light Sources");

				if (_bluredLightTexture == null)
				{
					int w = _lightSourcesTexture.width == _smallLightTextureSize.x ? _lightSourcesTexture.width * 2 : _lightSourcesTexture.width;
					int h = _lightSourcesTexture.height == _smallLightTextureSize.y ? _lightSourcesTexture.height * 2 : _lightSourcesTexture.height;
					_bluredLightTexture = new RenderTexture(w, h, 0, _texFormat);
				}

				_bluredLightTexture.DiscardContents();
				_lightSourcesTexture.filterMode = FilterMode.Bilinear;
				LightSourcesBlurMaterial.mainTexture = _lightSourcesTexture;
				Graphics.Blit(null, _bluredLightTexture, LightSourcesBlurMaterial);

				if (LightTexturesFilterMode == FilterMode.Point)
				{
					_lightSourcesTexture.filterMode = FilterMode.Point;
					_lightSourcesTexture.DiscardContents();
					Graphics.Blit(_bluredLightTexture, _lightSourcesTexture);
				}

				Profiler.EndSample();
			}
		}

		private void RenderAmbientLight()
		{
			if (!EnableAmbientLight || AmbientLightComputeMaterial == null)
			{
				return;
			}

			Profiler.BeginSample("LightingSystem.OnRenderImage Ambient Light");

			ConfigLightCamera(true);

			if (_ambientTexture == null)
			{
				_ambientTexture = new RenderTexture(_extendedLightTextureSize.x, _extendedLightTextureSize.y, 0, _texFormat);
			}
			if (_prevAmbientTexture == null)
			{
				_prevAmbientTexture = new RenderTexture(_extendedLightTextureSize.x, _extendedLightTextureSize.y, 0, _texFormat);
			}
			if (_ambientEmissionTexture == null)
			{
				_ambientEmissionTexture = new RenderTexture(_extendedLightTextureSize.x, _extendedLightTextureSize.y, 0, _texFormat);
			}

			if (EnableAmbientLight)
			{
				Color oldBackgroundColor = LightCamera.backgroundColor;
				LightCamera.targetTexture = _ambientEmissionTexture;
				LightCamera.cullingMask = 1 << AmbientLightLayer;
				LightCamera.backgroundColor = new Color(0, 0, 0, 0);
				LightCamera.Render();
				LightCamera.targetTexture = null;
				LightCamera.cullingMask = 0;
				LightCamera.backgroundColor = oldBackgroundColor;
			}

			for (int i = 0; i < _aditionalAmbientLightCycles + 1; i++)
			{
				RenderTexture tmp = _prevAmbientTexture;
				_prevAmbientTexture = _ambientTexture;
				_ambientTexture = tmp;

				Vector2 texSize = new Vector2(_ambientTexture.width, _ambientTexture.height);
				Vector2 posShift = ((Vector2) (_currPos - _oldPos) / LightPixelSize).Div(texSize);
				_oldPos = _currPos;

				AmbientLightComputeMaterial.SetTexture("_LightSourcesTex", _ambientEmissionTexture);
				AmbientLightComputeMaterial.SetTexture("_MainTex", _prevAmbientTexture);
				AmbientLightComputeMaterial.SetVector("_Shift", posShift);

				_ambientTexture.DiscardContents();
				Graphics.Blit(null, _ambientTexture, AmbientLightComputeMaterial);

				if (BlurAmbientLight && AmbientLightBlurMaterial != null)
				{
					Profiler.BeginSample("LightingSystem.OnRenderImage Bluring Ambient Light");

					_prevAmbientTexture.DiscardContents();
					AmbientLightBlurMaterial.mainTexture = _ambientTexture;
					Graphics.Blit(null, _prevAmbientTexture, AmbientLightBlurMaterial);

					RenderTexture tmpblur = _prevAmbientTexture;
					_prevAmbientTexture = _ambientTexture;
					_ambientTexture = tmpblur;

					Profiler.EndSample();
				}
			}

			_aditionalAmbientLightCycles = 0;
			Profiler.EndSample();
		}

		private void RenderLightOverlay(RenderTexture src, RenderTexture dest)
		{
			Profiler.BeginSample("LightingSystem.OnRenderImage Light Overlay");

			ConfigLightCamera(false);

			Vector2 lightTexelSize = new Vector2(1f / _smallLightTextureSize.x, 1f / _smallLightTextureSize.y);
			float lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;
			Vector2 worldOffset = Quaternion.Inverse(_camera.transform.rotation) * (LightCamera.transform.position - _camera.transform.position);
			Vector2 offset = Vector2.Scale(lightTexelSize, -worldOffset * lightPixelsPerUnityMeter);

			RenderTexture lightSourcesTex = BlurLightSources && LightSourcesBlurMaterial != null && LightTexturesFilterMode != FilterMode.Point
				? _bluredLightTexture
				: _lightSourcesTexture;
			float xDiff = _camera.aspect / LightCamera.aspect;

			if (!_camera.orthographic)
			{
				float gameCamHalfFov = _camera.fieldOfView * Mathf.Deg2Rad / 2f;
				float gameCamSize = Mathf.Tan(gameCamHalfFov) * LightObstaclesDistance * 2;
				_camera.orthographicSize = gameCamSize / 2f;
			}

			float scaleY = _camera.orthographicSize / LightCamera.orthographicSize;
			Vector2 scale = new Vector2(scaleY * xDiff, scaleY);

			FilterMode oldAmbientFilterMode = _ambientTexture == null ? FilterMode.Point : _ambientTexture.filterMode;
			LightOverlayMaterial.SetTexture("_AmbientLightTex", EnableAmbientLight ? _ambientTexture : null);
			LightOverlayMaterial.SetTexture("_LightSourcesTex", lightSourcesTex);
			LightOverlayMaterial.SetTexture("_GameTex", src);
			LightOverlayMaterial.SetVector("_Offset", offset);
			LightOverlayMaterial.SetVector("_Scale", scale);

			if (_screenBlitTempTex == null || _screenBlitTempTex.width != src.width || _screenBlitTempTex.height != src.height)
			{
				if (_screenBlitTempTex != null)
				{
					_screenBlitTempTex.Release();
				}
				_screenBlitTempTex = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32);
				_screenBlitTempTex.filterMode = FilterMode.Point;
			}

			_screenBlitTempTex.DiscardContents();
			Graphics.Blit(null, _screenBlitTempTex, LightOverlayMaterial);

			if (_ambientTexture != null)
			{
				_ambientTexture.filterMode = oldAmbientFilterMode;
			}

			Graphics.Blit(_screenBlitTempTex, dest);

			Profiler.EndSample();
		}

		private void UpdateCamera()
		{
			LightPixelSize = _camera.orthographicSize * 2f / _smallLightTextureSize.y;

			float lightPixelsPerUnityMeter = LightPixelsPerUnityMeter;
			Vector3 mainPos = _camera.transform.position;
			Quaternion camRot = _camera.transform.rotation;
			Vector3 unrotMainPos = Quaternion.Inverse(camRot) * mainPos;
			Vector2 gridPos = new Vector2(Mathf.Round(unrotMainPos.x * lightPixelsPerUnityMeter) / lightPixelsPerUnityMeter,
				Mathf.Round(unrotMainPos.y * lightPixelsPerUnityMeter) / lightPixelsPerUnityMeter);
			Vector2 posDiff = gridPos - (Vector2) unrotMainPos;
			Vector3 pos = camRot * posDiff + mainPos;
			LightCamera.transform.position = pos;
			_currPos = pos;
		}

		public void LoopAmbientLight(int cycles)
		{
			_aditionalAmbientLightCycles += cycles;
		}

		private void ConfigLightCamera(bool extended)
		{
			if (extended)
			{
				LightCamera.orthographicSize =
					_camera.orthographicSize *
					(_extendedLightTextureSize.y / (float) _smallLightTextureSize.y); // _extendedLightTextureSize.y/(2f*LightPixelsPerUnityMeter);
				LightCamera.fieldOfView = _camera.fieldOfView + LightCameraFovAdd;
				LightCamera.aspect = _extendedLightTextureSize.x / (float) _extendedLightTextureSize.y;
			}
			else
			{
				LightCamera.orthographicSize = _camera.orthographicSize; // _smallLightTextureSize.y / (2f * LightPixelsPerUnityMeter);
				LightCamera.fieldOfView = _camera.fieldOfView;
				LightCamera.aspect = _smallLightTextureSize.x / (float) _smallLightTextureSize.y;
			}
		}
	}
}