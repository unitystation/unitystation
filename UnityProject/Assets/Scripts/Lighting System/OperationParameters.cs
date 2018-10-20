using System;
using UnityEngine;

public struct OperationParameters : IEquatable<OperationParameters>
{
	public readonly float cameraOrthographicSize;
	public readonly Vector2Int screenSize;
	public readonly float cameraAspect;
	public readonly Vector3 cameraViewportUnitsInWorldSpace;
	public readonly PixelPerfectRTParameter occlusionPPRTParameter;
	public readonly PixelPerfectRTParameter fovPPRTParameter;
	public readonly PixelPerfectRTParameter lightPPRTParameter;
	public readonly PixelPerfectRTParameter obstacleLightPPRTParameter;

	private bool mExtendedDataCalculated;
	private float mExtendedCameraSize;
	private Vector2Int mCameraViewportUnitsCeiled;
	private Vector2Int mExtendedTextureSize;
	private int lightTextureWidth;
	private Vector2Int mLightTextureSize;
	private Vector3 cameraViewportUnits;


	public OperationParameters(Camera iCamera, RenderSettings iRenderSettings)
	{
		cameraOrthographicSize = iCamera.orthographicSize;
		screenSize = new Vector2Int(Screen.width, Screen.height);
		cameraAspect = iCamera.aspect;

		lightTextureWidth = iRenderSettings.lightTextureWidth;
		cameraViewportUnitsInWorldSpace = iCamera.WorldToViewportPoint(Vector3.zero) - iCamera.WorldToViewportPoint(Vector3.one);
		cameraViewportUnits = iCamera.ViewportToWorldPoint(Vector3.one) - iCamera.ViewportToWorldPoint(Vector3.zero);

		mCameraViewportUnitsCeiled = new Vector2Int(Mathf.CeilToInt(cameraViewportUnits.x), Mathf.CeilToInt(cameraViewportUnits.y));

		int _initialSampleDetail = iRenderSettings.occlusionDetail % 2 == 0 ? iRenderSettings.occlusionDetail : ++iRenderSettings.occlusionDetail;

		occlusionPPRTParameter = new PixelPerfectRTParameter(mCameraViewportUnitsCeiled + iRenderSettings.occlusionMaskSizeAdd, _initialSampleDetail);
		fovPPRTParameter = new PixelPerfectRTParameter(occlusionPPRTParameter.units, _initialSampleDetail * (int)iRenderSettings.fovResample);
		lightPPRTParameter = new PixelPerfectRTParameter(mCameraViewportUnitsCeiled, _initialSampleDetail * (int)iRenderSettings.lightResample);
		//lightOcclusionTextureSize = new Vector2Int((int)(lightPPRTParameter.resolution.x * iRenderSettings.occlusionLightTextureRescale), (int)(lightPPRTParameter.resolution.y * iRenderSettings.occlusionLightTextureRescale));
		obstacleLightPPRTParameter = new PixelPerfectRTParameter(lightPPRTParameter.units, Mathf.Clamp(_initialSampleDetail, 2, Int32.MaxValue));

		// Set data default.
		// This will be lazy-calculated when required.
		mExtendedDataCalculated = default(bool);
		mExtendedCameraSize = default(float);
		mExtendedTextureSize = default(Vector2Int);
		mLightTextureSize = default(Vector2Int);
	}

	public Vector2Int extendedTextureSize
	{
		get
		{
			if (mExtendedDataCalculated == false)
			{
				InitializeData();
			}

			return mExtendedTextureSize;
		}
	}

	public float extendedCameraSize
	{
		get
		{
			if (mExtendedDataCalculated == false)
			{
				InitializeData();
			}

			return mExtendedCameraSize;
		}
	}

	private Vector2Int cameraViewportUnitsCeiled
	{
		get
		{
			if (mExtendedDataCalculated == false)
			{
				InitializeData();
			}

			return mCameraViewportUnitsCeiled;
		}

		set
		{
			mCameraViewportUnitsCeiled = value;
		}
	}

	public void InitializeData()
	{

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

		float _lightToExtendedProportions = 1; // DefaultCameraSize + occlusionMaskSizeAdd.y) / DefaultCameraSize;

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
		       this.lightTextureWidth == iOperation.lightTextureWidth &&
		       this.occlusionPPRTParameter == iOperation.occlusionPPRTParameter &&
		       this.fovPPRTParameter == iOperation.fovPPRTParameter &&
		       this.lightPPRTParameter == iOperation.lightPPRTParameter &&
		       this.obstacleLightPPRTParameter == iOperation.obstacleLightPPRTParameter;
	}
}