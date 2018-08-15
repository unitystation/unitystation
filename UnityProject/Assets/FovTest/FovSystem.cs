using System;
using System.Collections;
using System.Collections.Generic;
using SuperBlur;
using UnityEngine;

/// <summary>
/// Component is Work in process.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FovSystem : MonoBehaviour
{
	public Material maskIntensifyMaterial;
	public Material maskBlitMaterial;
	public Material maskBlurMaterial;
	public Material maskPixelateMaterial;

	public RenderTexture globalOcclusionMask;

	public FovRenderSettings renderSettings;

	private const string MaskLayerName = "FieldOfViewMask";
	private const string MaskCameraName = "Mask Camera";

	private Camera mMainCamera;
	private FovMaskRenderer mFovMaskRenderer;
	private Vector2 mLastScreenSize;
	private float mLastOrthografic;

	private void Awake()
	{
		mMainCamera = gameObject.GetComponent<Camera>();

		if (mMainCamera == null)
			throw new ArgumentNullException("FovSystemManager require Camera component to operate.");

		mFovMaskRenderer = FovMaskRenderer.InitializeMaskRenderer(gameObject, mMainCamera, MaskLayerName, MaskCameraName, () => renderSettings);
	}

	private void OnRenderImage(RenderTexture iSource, RenderTexture iDestination)
	{
		if (mLastScreenSize != new Vector2(Screen.width, Screen.height) || globalOcclusionMask == null || mLastOrthografic != mMainCamera.orthographicSize)
		{
			mLastOrthografic = mMainCamera.orthographicSize;
			mLastScreenSize = new Vector2(Screen.width, Screen.height);
			globalOcclusionMask = new RenderTexture(Screen.width, Screen.height, 0);
			Shader.SetGlobalTexture("_FovMask", globalOcclusionMask);
		}

		if (maskBlitMaterial == null)
		{
			Debug.Log($"FovSystemManager: Unable to blit Fov mask. {nameof(maskBlitMaterial)} not provided.");
			return;
		}

		var _mask = mFovMaskRenderer.mask;


		if (_mask == null)
		{
			Graphics.Blit(iSource, iDestination);
			return;
		}

		globalOcclusionMask.DiscardContents();

		maskIntensifyMaterial.SetFloat("_CorrectionOffset", renderSettings.maskCorrection);
		Graphics.Blit(_mask, globalOcclusionMask, maskIntensifyMaterial);

		Blur(globalOcclusionMask, _mask);

		globalOcclusionMask.DiscardContents();
		Pixelate(_mask, globalOcclusionMask);


		maskBlitMaterial.SetTexture("_Mask", globalOcclusionMask);
		maskBlitMaterial.SetFloat("_Alpha", renderSettings.maskAlpha);
		Graphics.Blit(iSource, iDestination, maskBlitMaterial);
	}

	private void Blur(RenderTexture source, RenderTexture destination)
	{
		if (source == null)
			return;

		Shader.DisableKeyword("GAMMA_CORRECTION");
		
		var rt2 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

		for (int i = 0; i < renderSettings.blurIterations; i++)
		{
			// helps to achieve a larger blur
			float radius = (float)i * renderSettings.blurInterpolation + renderSettings.blurInterpolation;
			maskBlurMaterial.SetFloat("_Radius", radius);

			Graphics.Blit(source, rt2, maskBlurMaterial, 1);
			source.DiscardContents();

			// is it a last iteration? If so, then blit to destination
			if (i == renderSettings.blurIterations - 1)
			{
				Graphics.Blit(rt2, destination, maskBlurMaterial, 2);
			}
			else
			{
				Graphics.Blit(rt2, source, maskBlurMaterial, 2);
				rt2.DiscardContents();
			}
		}

		RenderTexture.ReleaseTemporary(rt2);
	}
	
	private void Pixelate(RenderTexture source, RenderTexture destination)
	{
		maskPixelateMaterial.SetInt("_PixelateX", renderSettings.pixelateSize);
		maskPixelateMaterial.SetInt("_PixelateY", renderSettings.pixelateSize);
		Graphics.Blit(source, destination, maskPixelateMaterial);
	}



}