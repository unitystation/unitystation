using System;
using UnityEngine;

public struct OperationParameters : IEquatable<OperationParameters>
{
	public readonly float cameraOrthographicSize;
	public readonly Vector2Int screenSize;
	public readonly float cameraAspect;
	public readonly Vector2Int occlusionMaskSizeAdd;
	public readonly int antiAliasing;
	public readonly float wallTextureRescale; // TODO RENAME.
	public readonly Vector3 cameraViewportUnitsInWorldSpace;

	private const float DefaultCameraSize = 4;

	private bool mExtendedDataCalculated;
	private float mExtendedCameraSize;
	private Vector2Int mCameraViewportUnitsCeiled;
	private Vector2Int mExtendedTextureSize;
	private int lightTextureWidth;
	private Vector2Int mLightTextureSize;
	private int occlusionMaskPixelsInUnit;
	private Vector3 cameraViewportUnits;

	public OperationParameters(Camera iCamera, RenderSettings iRenderSettings)
	{
		cameraOrthographicSize = iCamera.orthographicSize;
		screenSize = new Vector2Int(Screen.width, Screen.height);
		cameraAspect = iCamera.aspect;

		lightTextureWidth = iRenderSettings.lightTextureWidth;
		antiAliasing = Mathf.Clamp(iRenderSettings.antiAliasing, 1, 16);
		wallTextureRescale = iRenderSettings.occlusionLightTextureRescale;
		cameraViewportUnitsInWorldSpace = iCamera.WorldToViewportPoint(Vector3.zero) - iCamera.WorldToViewportPoint(Vector3.one);
		cameraViewportUnits = iCamera.ViewportToWorldPoint(Vector3.one) - iCamera.ViewportToWorldPoint(Vector3.zero);

		occlusionMaskSizeAdd = iRenderSettings.occlusionMaskSizeAdd;
		occlusionMaskPixelsInUnit = iRenderSettings.occlusionMaskPixelsInUnit;

		// Set data default.
		// This will be lazy-calculated when required.
		mExtendedDataCalculated = default(bool);
		mExtendedCameraSize = default(float);
		mExtendedTextureSize = default(Vector2Int);
		mLightTextureSize = default(Vector2Int);
		mCameraViewportUnitsCeiled = default(Vector2Int);
	}

	public Vector2Int extendedTextureSize
	{
		get
		{
			if (mExtendedDataCalculated == false)
			{
				CalculateExtendedData();
			}

			return mExtendedTextureSize;
		}
	}

	public Vector2Int lightTextureSize
	{
		get
		{
			if (mExtendedDataCalculated == false)
			{
				CalculateExtendedData();
			}

			return mLightTextureSize;
		}
	}

	public float extendedCameraSize
	{
		get
		{
			if (mExtendedDataCalculated == false)
			{
				CalculateExtendedData();
			}

			return mExtendedCameraSize;
		}
	}

	public PixelPerfectRTParameter occlusionPPRTParameter
	{
		get
		{
			return new PixelPerfectRTParameter(cameraViewportUnitsCeiled + occlusionMaskSizeAdd, occlusionMaskPixelsInUnit, cameraViewportUnits);
		}
	}

	private Vector2Int cameraViewportUnitsCeiled
	{
		get
		{
			if (mExtendedDataCalculated == false)
			{
				CalculateExtendedData();
			}

			return mCameraViewportUnitsCeiled;
		}

		set
		{
			mCameraViewportUnitsCeiled = value;
		}
	}

	private void CalculateExtendedData()
	{
		cameraViewportUnitsCeiled = new Vector2Int(Mathf.CeilToInt(cameraViewportUnits.x), Mathf.CeilToInt(cameraViewportUnits.y));

		// Light Texture.
		if (screenSize.x > screenSize.y)
		{
			float _widthAspect = (float)screenSize.x / screenSize.y;
			mLightTextureSize = new Vector2Int(lightTextureWidth, (int)(lightTextureWidth / _widthAspect));
		}
		else
		{
			float _highAspect = (float)screenSize.y / screenSize.x;
			mLightTextureSize = new Vector2Int((int)(lightTextureWidth / _highAspect), lightTextureWidth);
		}

		float _lightToExtendedProportions = (DefaultCameraSize + occlusionMaskSizeAdd.y) / DefaultCameraSize;

		// Extended Texture.
		mExtendedCameraSize = cameraOrthographicSize * _lightToExtendedProportions;

		mExtendedTextureSize = new Vector2Int((int)(mLightTextureSize.x * _lightToExtendedProportions), (int)(mLightTextureSize.y * _lightToExtendedProportions));

		mExtendedDataCalculated = true;
	}

	public static bool operator ==(OperationParameters iLeftHand, OperationParameters iRightHand)
	{
		// Equals handles case of null on right side.
		return iLeftHand.Equals(iRightHand);
	}

	public static bool operator !=(OperationParameters iLeftHand, OperationParameters iRightHand)
	{
		return !(iLeftHand == iRightHand);
	}

	public bool Equals(OperationParameters iOperation)
	{
		return this.cameraOrthographicSize == iOperation.cameraOrthographicSize &&
		       this.screenSize == iOperation.screenSize &&
			   this.cameraAspect == iOperation.cameraAspect &&
		       this.occlusionMaskSizeAdd == iOperation.occlusionMaskSizeAdd &&
		       this.lightTextureWidth == iOperation.lightTextureWidth &&
			   this.antiAliasing == iOperation.antiAliasing &&
			   this.wallTextureRescale == iOperation.wallTextureRescale &&
			   this.occlusionMaskPixelsInUnit == iOperation.occlusionMaskPixelsInUnit;
	}
}