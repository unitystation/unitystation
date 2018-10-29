﻿using System;
using UnityEngine;

public class PostProcessingStack
{
	private readonly MaterialContainer mMaterialContainer;
	private RenderTexture mBlurRenderTexture;
	private RenderTexture mBlurRenderTextureLight;
	private RenderTexture mBlurRenderTextureOccLight;

	public PostProcessingStack(MaterialContainer iMaterialContainer)
	{
		mMaterialContainer = iMaterialContainer;
	}

	private RenderTexture blurRenderTexture
	{
		get
		{
			return mBlurRenderTexture;
		}

		set
		{
			if (mBlurRenderTexture == value)
				return;

			if (mBlurRenderTexture != null)
			{
				mBlurRenderTexture.Release();
			}

			mBlurRenderTexture = value;
		}
	}

	private RenderTexture blurRenderTextureLight
	{
		get
		{
			return mBlurRenderTextureLight;
		}

		set
		{
			if (mBlurRenderTextureLight == value)
				return;

			if (mBlurRenderTextureLight != null)
			{
				mBlurRenderTextureLight.Release();
			}

			mBlurRenderTextureLight = value;
		}
	}

	private RenderTexture blurRenderTextureOccLight
	{
		get
		{
			return mBlurRenderTextureOccLight;
		}

		set
		{
			if (mBlurRenderTextureOccLight == value)
				return;

			if (mBlurRenderTextureOccLight != null)
			{
				mBlurRenderTextureOccLight.Release();
			}

			mBlurRenderTextureOccLight = value;
		}
	}

	/// <summary>	
	/// Processes provided Mask thru post processing stack.
	/// </summary>
	/// <param name="iMask">Raw Occlusion mask with R and G channels.</param>
	/// <param name="iDestination">Write Destination texture.</param>
	/// <param name="iRenderSettings">Settings to use</param>
	public void BlurOcclusionMask(RenderTexture iMask, RenderSettings iRenderSettings, float iCameraSize)
	{
		// Blur G channel only.
		Blur(iMask, mMaterialContainer.fovBlurMaterial, iRenderSettings.fovBlurInterpolation, iRenderSettings.fovBlurIterations, blurRenderTexture, iCameraSize);
	}

	public void BlurLightMask(
		RenderTexture iMask,
		RenderSettings iRenderSettings,
		float iCameraSize,
		float iMatrixRotationModeBlend)
	{
		// In case of matrix rotation we want to blur more to hide quirks.
		float _interpolation = Mathf.Lerp(iRenderSettings.lightBlurInterpolation, iRenderSettings.lightBlurInterpolation * 4, iMatrixRotationModeBlend);
 
		Blur(iMask, mMaterialContainer.lightBlurMaterial, _interpolation, iRenderSettings.lightBlurIterations, blurRenderTextureLight, iCameraSize);
	}

	public void BlurOcclusionMaskRotation(
		RenderTexture iMask,
		RenderSettings iRenderSettings,
		float iCameraSize,
		float iMatrixRotationModeBlend)
	{
		float _interpolation = Mathf.Lerp(0, iRenderSettings.rotationBlurInterpolation, iMatrixRotationModeBlend);

		Blur(iMask, mMaterialContainer.lightBlurMaterial, _interpolation, iRenderSettings.rotationBlurIterations, blurRenderTextureLight, iCameraSize);
	}

	public void ResetRenderingTextures(OperationParameters iParameters)
	{
		// Prepare and assign RenderTexture.
		int _textureWidth = iParameters.screenSize.x;
		int _textureHeight = iParameters.screenSize.y;

		{
			var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
			_newRenderTexture.name = "Blur Render Texture";
			_newRenderTexture.autoGenerateMips = false;
			_newRenderTexture.useMipMap = false;

			// Note: Assignment will release previous texture if exist.
			blurRenderTexture = _newRenderTexture;
		}

		{
			var _newRenderTexture = new RenderTexture(iParameters.lightPPRTParameter.resolution.x, iParameters.lightPPRTParameter.resolution.y, 0, RenderTextureFormat.Default);
			_newRenderTexture.name = "Blur Render Texture";
			_newRenderTexture.autoGenerateMips = false;
			_newRenderTexture.useMipMap = false;

			// Note: Assignment will release previous texture if exist.
			blurRenderTextureLight = _newRenderTexture;
		}

		{
			var _newRenderTexture = new RenderTexture(iParameters.obstacleLightPPRTParameter.resolution.x, iParameters.obstacleLightPPRTParameter.resolution.y, 0, RenderTextureFormat.Default);
			_newRenderTexture.name = "Blur Render Texture";
			_newRenderTexture.autoGenerateMips = false;
			_newRenderTexture.useMipMap = false;

			// Note: Assignment will release previous texture if exist.
			blurRenderTextureOccLight = _newRenderTexture;
		}
	}

	public void GenerateFovMask(
		PixelPerfectRT iRawOcclusionMask,
		PixelPerfectRT iGlobalOcclusionExtendedMask,
		RenderSettings iRenderSettings,
		Vector3 iFovCenterInViewSpace,
		float iFovDistance,
		OperationParameters iOperationParameters)
	{
		mMaterialContainer.fovMaterial.SetVector("_PositionOffset", iFovCenterInViewSpace);
		mMaterialContainer.fovMaterial.SetFloat("_OcclusionSpread", iRenderSettings.fovOcclusionSpread);

		// Adjust scale from Extended mask to Screen size mask.
		float _yUVScale = 1 / ((float)iGlobalOcclusionExtendedMask.renderTexture.width / iGlobalOcclusionExtendedMask.renderTexture.height);
		Vector3 _adjustedDistance = iFovDistance * iOperationParameters.cameraViewportUnitsInWorldSpace * iRawOcclusionMask.orthographicSize / iGlobalOcclusionExtendedMask.orthographicSize;

		mMaterialContainer.fovMaterial.SetVector("_Distance", new Vector3(_adjustedDistance.x, _yUVScale,  iRenderSettings.fovHorizonSmooth));

		iRawOcclusionMask.renderTexture.filterMode = FilterMode.Bilinear;
		PixelPerfectRT.Blit(iRawOcclusionMask, iGlobalOcclusionExtendedMask, mMaterialContainer.fovMaterial);
	}

	public void CreateWallLightMask(
		PixelPerfectRT iLightMask,
		PixelPerfectRT iObstacleLightMask,
		RenderSettings iRenderSettings,
		float iCameraSize)
	{
		// Down Scale light mask and blur it.
		PixelPerfectRT.Transform(iLightMask, iObstacleLightMask, mMaterialContainer.PPRTTransformMaterial);

		mMaterialContainer.lightWallBlurMaterial.SetVector("_MultiLimit", new Vector4(iRenderSettings.occlusionMaskMultiplier,iRenderSettings.occlusionMaskLimit,0,0));

		Blur(iObstacleLightMask.renderTexture, mMaterialContainer.lightWallBlurMaterial, iRenderSettings.occlusionBlurInterpolation, iRenderSettings.occlusionBlurIterations, blurRenderTextureOccLight, iCameraSize);
	}

	private static bool Blur(
		RenderTexture iSource,
		Material iBlurMaterial,
		float iInterpolation,
		int iIteration,
		RenderTexture iBlurRenderTexture,
		float iCameraSize)
	{
		const float DefaultCameraSize = 4f;

		if (iSource == null)
			throw new ArgumentNullException(nameof(iSource));

		if (iIteration == 0 || Math.Abs(iInterpolation) < 0.0001)
			return false;

		if (iBlurMaterial == null)
		{
			UnityEngine.Debug.LogError($"PostProcessingStack: Unable to do a blur pass. Provided material is null.");
			return false;
		}

		float _cameraSizeInterpolationMultiplier = DefaultCameraSize / iCameraSize;

		for (int _iteration = 0; _iteration < iIteration; _iteration++)
		{
			// Helps to achieve a larger blur.
			float _blurRadius = ((_iteration * iInterpolation) + iInterpolation) * 0.005f * _cameraSizeInterpolationMultiplier;

			iBlurMaterial.SetFloat("_Radius", _blurRadius);

			iSource.filterMode = FilterMode.Bilinear;
			iBlurRenderTexture.filterMode = FilterMode.Bilinear;

			Graphics.Blit(iSource, iBlurRenderTexture, iBlurMaterial, 1);
			iSource.DiscardContents();

			// is it a last iteration? If so, then blit to destination
			if (_iteration == iIteration - 1)
			{
				Graphics.Blit(iBlurRenderTexture, iSource, iBlurMaterial, 2);
			}
			else
			{
				Graphics.Blit(iBlurRenderTexture, iSource, iBlurMaterial, 2);
				iBlurRenderTexture.DiscardContents();
			}
		}

		return true;
	}
}