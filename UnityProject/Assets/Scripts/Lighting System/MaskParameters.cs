using System;
using UnityEngine;

public struct MaskParameters : IEquatable<MaskParameters>
{
	public readonly float cameraOrthographicSize;
	public readonly Vector2Int screenSize;
	public readonly float cameraAspect;
	public readonly float maskCameraSizeAdd;
	public readonly int antiAliasing;
	public readonly float wallTextureRescale;
	public readonly Vector3 worldUnitInViewportSpace;

	private const float DefaultCameraSize = 4;

	private bool mExtendedDataCalculated;
	private float mExtendedCameraSize;
	private Vector2Int mExtendedTextureSize;
	private int lightTextureWidth;
	private Vector2Int mLightTextureSize;

	public MaskParameters(Camera iCamera, RenderSettings iRenderSettings)
	{
		cameraOrthographicSize = iCamera.orthographicSize;
		screenSize = new Vector2Int(Screen.width, Screen.height);
		cameraAspect = iCamera.aspect;
		maskCameraSizeAdd = iRenderSettings.maskCameraSizeAdd;
		lightTextureWidth = iRenderSettings.lightTextureWidth;
		antiAliasing = Mathf.Clamp(iRenderSettings.antiAliasing, 1, 16);
		wallTextureRescale = iRenderSettings.occlusionLightTextureRescale;
		worldUnitInViewportSpace = iCamera.WorldToViewportPoint(Vector3.zero) - iCamera.WorldToViewportPoint(Vector3.one);

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

	private void CalculateExtendedData()
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

		float _lightToExtendedProportions = (DefaultCameraSize + maskCameraSizeAdd) / DefaultCameraSize;

		// Extended Texture.
		mExtendedCameraSize = cameraOrthographicSize * _lightToExtendedProportions;

		mExtendedTextureSize = new Vector2Int((int)(mLightTextureSize.x * _lightToExtendedProportions), (int)(mLightTextureSize.y * _lightToExtendedProportions));

		mExtendedDataCalculated = true;
	}

	public static bool operator ==(MaskParameters iLeftHand, MaskParameters iRightHand)
	{
		// Equals handles case of null on right side.
		return iLeftHand.Equals(iRightHand);
	}

	public static bool operator !=(MaskParameters iLeftHand, MaskParameters iRightHand)
	{
		return !(iLeftHand == iRightHand);
	}

	public bool Equals(MaskParameters iMask)
	{
		return this.cameraOrthographicSize == iMask.cameraOrthographicSize &&
		       this.screenSize == iMask.screenSize &&
			   this.cameraAspect == iMask.cameraAspect &&
		       this.maskCameraSizeAdd == iMask.maskCameraSizeAdd &&
		       this.lightTextureWidth == iMask.lightTextureWidth &&
			   this.antiAliasing == iMask.antiAliasing &&
			   this.wallTextureRescale == iMask.wallTextureRescale;
	}
}