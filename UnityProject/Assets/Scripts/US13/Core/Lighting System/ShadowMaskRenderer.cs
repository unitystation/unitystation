using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace US13.Core.Lighting_System
{
	public class ShadowMaskRenderer : MonoBehaviour
	{
		public Camera mTableMaskCamera;
		public Camera mWallMaskCamera; //It is because the other one is pixel perfect for some reason
		public Camera mShadowCamera;
		private Vector3 mPreviousCameraPosition;
		private Vector2 mPreviousFilteredPosition;

		private RenderTexture m_pingRT;
		private RenderTexture m_pongRT;
		private RenderTexture m_tableRT;
		private RenderTexture m_WallRT;
		private RenderTexture m_itemRT;

		[Header("Debug")]
		[Tooltip("Draws the intermediate render textures in the top-left corner of the Game view.")]
		public bool debugDrawTextures;

		public RenderTexture _WallRT
		{
			get { return m_WallRT; }
			set { Replace(ref m_WallRT, value); }
		}

		private RenderTexture _pingRT
		{
			get { return m_pingRT; }
			set { Replace(ref m_pingRT, value); }
		}

		public RenderTexture _ItemRT
		{
			get { return m_itemRT; }
			set { Replace(ref m_itemRT, value); }
		}

		public RenderTexture _pongRT
		{
			get { return m_pongRT; }
			set { Replace(ref m_pongRT, value); }
		}

		private RenderTexture _tableRT
		{
			get { return m_tableRT; }
			set { Replace(ref m_tableRT, value); }
		}

		[Header("Shadow — Shaders")]
		[Tooltip("ShadowSilhouette.shader — renders casters as flat black into the shadow RT.")]
		public Shader   silhouetteShader;
		[Tooltip("Material using ShadowBlur.shader — separable Gaussian blur + classification pass.")]
		public Material blurMaterial;

		public int   blurPasses = 2;

		public static float shadowAlpha = 0.4f;

		public static float blurSpread = 10f;

		/// <summary>
		/// Releases AND destroys the old texture. Release() alone only frees GPU memory and leaks the object.
		/// </summary>
		private static void Replace(ref RenderTexture iField, RenderTexture iNew)
		{
			if (iField != null && iField != iNew)
			{
				iField.Release();
				Destroy(iField);
			}

			iField = iNew;
		}

		private void OnDestroy()
		{
			_tableRT = null;
			_ItemRT = null;
			_pongRT = null;
			_pingRT = null;
			_WallRT = null;
		}

		public void SetUp(GameObject iRoot)
		{
			// Setup camera based on main camera.
			SetUpCameraObject(mTableMaskCamera);
			SetUpCameraObject(mShadowCamera);
			SetUpCameraObject(mWallMaskCamera);
		}

		private static RenderTexture CreateRT(string iName, int iWidth, int iHeight, RenderTextureFormat iFormat)
		{
			var _rt = new RenderTexture(iWidth, iHeight, 24, iFormat);
			_rt.name = iName;
			_rt.autoGenerateMips = false;
			_rt.useMipMap = false;
			_rt.filterMode = FilterMode.Point;
			return _rt;
		}

		public void ResetRenderingTextures(OperationParameters iParameters)
		{
			// Prepare and assign RenderTexture. (All have a 24-bit depth buffer, required for camera output in URP.)
			int _textureWidth = iParameters.screenSize.x;
			int _textureHeight = iParameters.screenSize.y;

			_tableRT = CreateRT("Table Mask", _textureWidth, _textureHeight, RenderTextureFormat.Default);
			_ItemRT = CreateRT("Item", _textureWidth, _textureHeight, RenderTextureFormat.ARGB32);
			_pongRT = CreateRT("pong", _textureWidth, _textureHeight, RenderTextureFormat.Default);
			_pingRT = CreateRT("ping", _textureWidth, _textureHeight, RenderTextureFormat.Default);
			_WallRT = CreateRT("_WallRT", _textureWidth, _textureHeight, RenderTextureFormat.Default);

			mShadowCamera.rect = iParameters.Rect;
			mTableMaskCamera.rect = iParameters.Rect;
			mWallMaskCamera.rect = iParameters.Rect;
			mTableMaskCamera.orthographicSize = iParameters.cameraOrthographicSize;
			mShadowCamera.orthographicSize = iParameters.cameraOrthographicSize;
			mWallMaskCamera.orthographicSize = iParameters.cameraOrthographicSize;
		}

		/// <summary>
		/// Must be called outside the render loop (LightingSystem calls it from LateUpdate).
		/// </summary>
		public void Render(Camera iCameraToMatch)
		{
			PrepareCamera(mShadowCamera, iCameraToMatch);
			PrepareCamera(mTableMaskCamera, iCameraToMatch);
			PrepareCamera(mWallMaskCamera, iCameraToMatch);

			RenderCamera(mShadowCamera, _ItemRT);
			RenderCamera(mTableMaskCamera, _tableRT);
			RenderCamera(mWallMaskCamera, _WallRT);

			blurMaterial.SetTexture("_WallFloorOcclusionTex", _WallRT);
			blurMaterial.SetTexture("_TableOcclusionTex",     _tableRT);

			Graphics.Blit(_ItemRT, _pongRT, blurMaterial, 1);

			for (int i = 0; i < blurPasses; i++)
			{
				blurMaterial.SetVector("_Direction", new Vector4(1, 0, 0, 0));
				blurMaterial.SetFloat ("_Spread",    blurSpread /iCameraToMatch.orthographicSize );
				Graphics.Blit(_pongRT, _pingRT, blurMaterial, 0);

				blurMaterial.SetVector("_Direction", new Vector4(0, 1, 0, 0));
				Graphics.Blit(_pingRT, _pongRT, blurMaterial, 0);
			}
		}

		private static void PrepareCamera(Camera iCamera, Camera iCameraToMatch)
		{
			iCamera.rect = iCameraToMatch.rect;
			iCamera.enabled = false;
			iCamera.backgroundColor = new Color(0, 0, 0, 0);
			iCamera.aspect = iCameraToMatch.aspect;
		}

		private static void RenderCamera(Camera iCamera, RenderTexture iTarget)
		{
			// The request supplies the destination, so the camera must not also have a target texture.
			iCamera.targetTexture = null;
			LightingSystem.RenderToTexture(iCamera, iTarget);
		}

		private void SetUpCameraObject(Camera iSetupCamera)
		{
			iSetupCamera.transform.localPosition = Vector3.zero;
			iSetupCamera.transform.localScale = Vector3.one;
			iSetupCamera.transform.localEulerAngles = Vector3.zero;

			iSetupCamera.orthographic = true;
			iSetupCamera.clearFlags = CameraClearFlags.Color;
			iSetupCamera.backgroundColor = Color.clear;
			iSetupCamera.depth = 9;
			iSetupCamera.allowHDR = false;
			iSetupCamera.allowMSAA = false;

			iSetupCamera.nearClipPlane = -3f;
			iSetupCamera.farClipPlane = 3f;

			// Render requests only work on Base cameras. Skip anything these masks don't need.
			var _data = iSetupCamera.GetUniversalAdditionalCameraData();
			_data.renderType = CameraRenderType.Base;
			_data.renderPostProcessing = false;
			_data.renderShadows = false;
		}

		private void OnGUI()
		{
			if (!debugDrawTextures || Event.current.type != EventType.Repaint)
				return;

			const float size = 200f;
			DrawDebug(_ItemRT, 0, size);
			DrawDebug(_tableRT, 1, size);
			DrawDebug(_WallRT, 2, size);
			DrawDebug(_pongRT, 3, size);
		}

		private static void DrawDebug(RenderTexture iTexture, int iIndex, float iSize)
		{
			if (iTexture == null)
				return;

			GUI.DrawTexture(new Rect(iIndex * (iSize + 4), 0, iSize, iSize), iTexture, ScaleMode.ScaleToFit, false);
		}
	}
}