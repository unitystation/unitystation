using System;
using UnityEngine;

public class PostProcessingStack
{
	private readonly FovMaterialContainer mMaterialContainer;
	private RenderTexture mBlurRenderTexture;
	private RenderTexture mBlurRenderTextureLight;
	private RenderTexture mBlurRenderTextureWallLight;

	public PostProcessingStack(FovMaterialContainer iMaterialContainer)
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
	public void PostProcessMask(RenderTexture iMask, RenderTexture iDestination, FovRenderSettings iRenderSettings)
	{
		// Blur G channel.
		Blur(iMask, mMaterialContainer.fovBlurMaterial, iRenderSettings.fovBlurInterpolation, iRenderSettings.fovBlurIterations, blurRenderTexture);

		// Pixelate G channel.
		// Note: Pixelation is done in one pass so it requires a destination texture.
		bool _pixelatePassSuccess = Pixelate(iMask, iDestination, iRenderSettings);

		// Blit mask to destination in case pixelation is failed.
		if (_pixelatePassSuccess == false)
		{
			Graphics.Blit(iMask, iDestination);
		}
	}

	public void PostProcessLightMask(RenderTexture iMask, RenderTexture iDestination, FovRenderSettings iRenderSettings)
	{
				// Wall Bleed
		Blur(iMask, mMaterialContainer.lightWallBlurMaterial, iRenderSettings.wallBlurInterpolation, iRenderSettings.wallBlurIterations, blurRenderTextureWallLight);


		// Blur G channel.
		Blur(iMask, mMaterialContainer.lightBlurMaterial, iRenderSettings.lightBlurInterpolation, iRenderSettings.lightBlurIterations, blurRenderTextureLight);



		Graphics.Blit(iMask, iDestination);
		return;
		// Pixelate G channel.
		// Note: Pixelation is done in one pass so it requires a destination texture.
		bool _pixelatePassSuccess = Pixelate(iMask, iDestination, iRenderSettings);

		// Blit mask to destination in case pixelation is failed.
		if (_pixelatePassSuccess == false)
		{
			Graphics.Blit(iMask, iDestination);
		}
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
		RenderTexture iBlurRenderTexture)
	{
		if (iSource == null)
			throw new ArgumentNullException(nameof(iSource));

		if (iIteration == 0 || Math.Abs(iInterpolation) < 0.0001)
			return false;

		if (iBlurMaterial == null)
		{
			UnityEngine.Debug.LogError($"PostProcessingStack: Unable to do a blur pass. Provided material is null.");
			return false;
		}

		for (int _iteration = 0; _iteration < iIteration; _iteration++)
		{
			// Helps to achieve a larger blur.
			float _blurRadius = (_iteration * iInterpolation) + iInterpolation;

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
	
	private bool Pixelate(RenderTexture iSource, RenderTexture iDestination, FovRenderSettings iRenderSettings)
	{
		if (iSource == null)
			throw new ArgumentNullException(nameof(iSource));

		if (iRenderSettings == null)
			throw new ArgumentNullException(nameof(iRenderSettings));

		if (iRenderSettings.pixelateSize == 0)
			return false;

		if (mMaterialContainer.fovPixelateMaterial == null)
		{
			UnityEngine.Debug.LogError($"PostProcessingStack: Unable to do a blur pass. {nameof(mMaterialContainer.fovPixelateMaterial)} is null.");
			return false;
		}

		mMaterialContainer.fovPixelateMaterial.SetInt("_Pixelate", iRenderSettings.pixelateSize);
		Graphics.Blit(iSource, iDestination, mMaterialContainer.fovPixelateMaterial);

		return true;
	}
}