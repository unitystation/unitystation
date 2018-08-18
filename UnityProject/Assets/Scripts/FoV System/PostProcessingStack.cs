using System;
using UnityEngine;

public class PostProcessingStack
{
	private readonly FovMaterialContainer mMaterialContainer;
	private RenderTexture mBlurRenderTexture;

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

	/// <summary>	
	/// Processes provided Mask thru post processing stack.
	/// </summary>
	/// <param name="iMask">Raw Occlusion mask with R and G channels.</param>
	/// <param name="iDestination">Write Destination texture.</param>
	/// <param name="iRenderSettings">Settings to use</param>
	public void PostProcessMask(RenderTexture iMask, RenderTexture iDestination, FovRenderSettings iRenderSettings)
	{
		// Blur G channel.
		Blur(iMask, iRenderSettings);

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

		var _newRenderTexture = new RenderTexture(_textureWidth, _textureHeight, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Blur Render Texture";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;

		// Note: Assignment will release previous texture if exist.
		blurRenderTexture = _newRenderTexture;
	}

	private bool Blur(RenderTexture iSource, FovRenderSettings iRenderSettings)
	{
		if (iSource == null)
			throw new ArgumentNullException(nameof(iSource));

		if (iRenderSettings == null)
			throw new ArgumentNullException(nameof(iRenderSettings));

		if (iRenderSettings.blurIterations == 0 || Math.Abs(iRenderSettings.blurInterpolation) < 0.0001)
			return false;

		if (mMaterialContainer.blurMaterial == null)
		{
			UnityEngine.Debug.LogError($"PostProcessingStack: Unable to do a blur pass. {nameof(mMaterialContainer.blurMaterial)} is null.");
			return false;
		}

		for (int _iteration = 0; _iteration < iRenderSettings.blurIterations; _iteration++)
		{
			// Helps to achieve a larger blur.
			float _blurRadius = (_iteration * iRenderSettings.blurInterpolation) + iRenderSettings.blurInterpolation;

			mMaterialContainer.blurMaterial.SetFloat("_Radius", _blurRadius);

			Graphics.Blit(iSource, blurRenderTexture, mMaterialContainer.blurMaterial, 1);
			iSource.DiscardContents();

			// is it a last iteration? If so, then blit to destination
			if (_iteration == iRenderSettings.blurIterations - 1)
			{
				Graphics.Blit(blurRenderTexture, iSource, mMaterialContainer.blurMaterial, 2);
			}
			else
			{
				Graphics.Blit(blurRenderTexture, iSource, mMaterialContainer.blurMaterial, 2);
				blurRenderTexture.DiscardContents();
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

		if (mMaterialContainer.pixelateMaterial == null)
		{
			UnityEngine.Debug.LogError($"PostProcessingStack: Unable to do a blur pass. {nameof(mMaterialContainer.pixelateMaterial)} is null.");
			return false;
		}

		mMaterialContainer.pixelateMaterial.SetInt("_Pixelate", iRenderSettings.pixelateSize);
		Graphics.Blit(iSource, iDestination, mMaterialContainer.pixelateMaterial);

		return true;
	}
}