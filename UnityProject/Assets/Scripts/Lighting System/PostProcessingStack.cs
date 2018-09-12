using System;
using UnityEngine;

public class PostProcessingStack
{
	private readonly MaterialContainer mMaterialContainer;
	private RenderTexture mBlurRenderTexture;
	private RenderTexture mBlurRenderTextureLight;
	private RenderTexture mBlurRenderTextureWallLight;

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

	private RenderTexture blurRenderTextureWallLight
	{
		get
		{
			return mBlurRenderTextureWallLight;
		}

		set
		{
			if (mBlurRenderTextureWallLight == value)
				return;

			if (mBlurRenderTextureWallLight != null)
			{
				mBlurRenderTextureWallLight.Release();
			}

			mBlurRenderTextureWallLight = value;
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
		float iCameraSize)
	{
		Blur(iMask, mMaterialContainer.lightBlurMaterial, iRenderSettings.lightBlurInterpolation, iRenderSettings.lightBlurIterations, blurRenderTextureLight, iCameraSize);

		return;
	}

	public void ResetRenderingTextures(MaskParameters iParameters)
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
			var _newRenderTexture = new RenderTexture(iParameters.lightTextureSize.x, iParameters.lightTextureSize.y, 0, RenderTextureFormat.Default);
			_newRenderTexture.name = "Blur Render Texture";
			_newRenderTexture.autoGenerateMips = false;
			_newRenderTexture.useMipMap = false;

			// Note: Assignment will release previous texture if exist.
			blurRenderTextureLight = _newRenderTexture;
		}

		{
			var _newRenderTexture = new RenderTexture(iParameters.lightTextureSize.x, iParameters.lightTextureSize.y, 0, RenderTextureFormat.Default);
			_newRenderTexture.name = "Blur Render Texture";
			_newRenderTexture.autoGenerateMips = false;
			_newRenderTexture.useMipMap = false;

			// Note: Assignment will release previous texture if exist.
			blurRenderTextureWallLight = _newRenderTexture;
		}
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

	public void GenerateFovMask(
		RenderTexture iRawOcclusionMask,
		RenderTexture iGlobalOcclusionExtendedMask,
		RenderSettings iRenderSettings,
		Vector3 iFovCenterInViewSpace,
		float iFovDistance,
		MaskParameters iMaskParameters)
	{
		mMaterialContainer.fovMaterial.SetVector("_PositionOffset", iFovCenterInViewSpace);
		mMaterialContainer.fovMaterial.SetFloat("_OcclusionSpread", iRenderSettings.fovOcclusionSpread);

		// Adjust scale from Extended mask to Screen size mask.
		float _yUVScale = 1 / iMaskParameters.cameraAspect;
		Vector3 _adjustedDistance = iFovDistance * iMaskParameters.worldUnitInViewportSpace * (float)iMaskParameters.cameraOrthographicSize / iMaskParameters.extendedCameraSize;

		mMaterialContainer.fovMaterial.SetVector("_Distance", new Vector3(_adjustedDistance.x, _yUVScale,  iRenderSettings.fovHorizonSmooth));
		
		Graphics.Blit(iRawOcclusionMask, iGlobalOcclusionExtendedMask, mMaterialContainer.fovMaterial);
	}

	public void FitExtendedOcclusionMask(RenderTexture iSource, RenderTexture iDestination, MaskParameters iMaskParameters)
	{
		Vector2 _scale = new Vector2((float)iMaskParameters.cameraOrthographicSize / iMaskParameters.extendedCameraSize, (float)iMaskParameters.cameraOrthographicSize / iMaskParameters.extendedCameraSize);

		Graphics.Blit(iSource, iDestination, _scale, (Vector2.one - _scale) * 0.5f);
	}
	
	public void CreateWallLightMask(
		RenderTexture iLightMask,
		RenderTexture iObstacleLightMask,
		RenderSettings iRenderSettings,
		float iCameraSize)
	{
		// Down Scale light mask and blur it.
		Graphics.Blit(iLightMask, iObstacleLightMask);

		mMaterialContainer.lightWallBlurMaterial.SetVector("_MultiLimit", new Vector4(iRenderSettings.occlusionMaskMultiplier,iRenderSettings.occlusionMaskLimit,0,0));

		Blur(iObstacleLightMask, mMaterialContainer.lightWallBlurMaterial, iRenderSettings.occlusionBlurInterpolation, iRenderSettings.occlusionBlurIterations, blurRenderTextureWallLight, iCameraSize);
	}
}