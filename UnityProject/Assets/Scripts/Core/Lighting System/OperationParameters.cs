using System;
using UnityEngine;

public struct OperationParameters : IEquatable<OperationParameters>
{
	public readonly float cameraOrthographicSize;
	public readonly Vector2Int screenSize;
	public readonly Vector3 cameraViewportUnitsInWorldSpace;
	public readonly PixelPerfectRTParameter occlusionPPRTParameter;
	public readonly PixelPerfectRTParameter fovPPRTParameter;
	public readonly PixelPerfectRTParameter lightPPRTParameter;
	public readonly PixelPerfectRTParameter obstacleLightPPRTParameter;
	private readonly Vector2Int cameraViewportUnitsCeiled;
	private readonly Vector3 cameraViewportUnits;

	public OperationParameters(Camera iCamera, RenderSettings iRenderSettings, bool iMatrixRotationMode)
	{
		cameraOrthographicSize = iCamera.orthographicSize;
		screenSize = new Vector2Int(Screen.width, Screen.height);

		cameraViewportUnitsInWorldSpace = iCamera.WorldToViewportPoint(Vector3.zero) - iCamera.WorldToViewportPoint(Vector3.one);
		cameraViewportUnits = iCamera.ViewportToWorldPoint(Vector3.one) - iCamera.ViewportToWorldPoint(Vector3.zero);
		cameraViewportUnitsCeiled = new Vector2Int(Mathf.CeilToInt(cameraViewportUnits.x), Mathf.CeilToInt(cameraViewportUnits.y));

		bool _highViewMode = PlayerPrefs.GetInt("CamZoomSetting") == 1;

		float _bla = screenSize.x / cameraViewportUnits.x;
		// Override Occlusion detail to match sprite detail if matrix is rotating.
		int _occlusionDetail = iMatrixRotationMode == false ? iRenderSettings.occlusionDetail : 32;

		// Reduce detail if player zoomed out.
		_occlusionDetail = _highViewMode == false ? _occlusionDetail : _occlusionDetail / 2;

		// Make sure detail is even value.
		int _initialSampleDetail = iRenderSettings.occlusionDetail % 2 == 0 ? _occlusionDetail : ++_occlusionDetail;

		occlusionPPRTParameter = new PixelPerfectRTParameter(cameraViewportUnitsCeiled + iRenderSettings.occlusionMaskSizeAdd, _initialSampleDetail);
		fovPPRTParameter = new PixelPerfectRTParameter(occlusionPPRTParameter.units, _initialSampleDetail);
		lightPPRTParameter = new PixelPerfectRTParameter(cameraViewportUnitsCeiled, _initialSampleDetail * (int)iRenderSettings.lightResample);
		obstacleLightPPRTParameter = new PixelPerfectRTParameter(lightPPRTParameter.units, Mathf.Clamp(_initialSampleDetail, 2, int.MaxValue));

		pixelsPerUnit = _occlusionDetail;
	}

	public int pixelsPerUnit { get; }

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
		return cameraOrthographicSize.Equals(iOperation.cameraOrthographicSize) &&
		       screenSize.Equals(iOperation.screenSize) &&
		       occlusionPPRTParameter.Equals(iOperation.occlusionPPRTParameter) &&
		       fovPPRTParameter.Equals(iOperation.fovPPRTParameter) &&
		       lightPPRTParameter.Equals(iOperation.lightPPRTParameter) &&
		       obstacleLightPPRTParameter.Equals(iOperation.obstacleLightPPRTParameter);
	}

	public override bool Equals(object obj)
	{
		return obj is OperationParameters other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = cameraOrthographicSize.GetHashCode();
			hashCode = (hashCode * 397) ^ screenSize.GetHashCode();
			hashCode = (hashCode * 397) ^ occlusionPPRTParameter.GetHashCode();
			hashCode = (hashCode * 397) ^ fovPPRTParameter.GetHashCode();
			hashCode = (hashCode * 397) ^ lightPPRTParameter.GetHashCode();
			hashCode = (hashCode * 397) ^ obstacleLightPPRTParameter.GetHashCode();
			return hashCode;
		}
	}
}