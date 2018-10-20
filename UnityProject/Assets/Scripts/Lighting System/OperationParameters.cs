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

	public OperationParameters(Camera iCamera, RenderSettings iRenderSettings)
	{
		cameraOrthographicSize = iCamera.orthographicSize;
		screenSize = new Vector2Int(Screen.width, Screen.height);

		cameraViewportUnitsInWorldSpace = iCamera.WorldToViewportPoint(Vector3.zero) - iCamera.WorldToViewportPoint(Vector3.one);
		cameraViewportUnits = iCamera.ViewportToWorldPoint(Vector3.one) - iCamera.ViewportToWorldPoint(Vector3.zero);
		cameraViewportUnitsCeiled = new Vector2Int(Mathf.CeilToInt(cameraViewportUnits.x), Mathf.CeilToInt(cameraViewportUnits.y));

		int _initialSampleDetail = iRenderSettings.occlusionDetail % 2 == 0 ? iRenderSettings.occlusionDetail : ++iRenderSettings.occlusionDetail;

		occlusionPPRTParameter = new PixelPerfectRTParameter(cameraViewportUnitsCeiled + iRenderSettings.occlusionMaskSizeAdd, _initialSampleDetail);
		fovPPRTParameter = new PixelPerfectRTParameter(occlusionPPRTParameter.units, _initialSampleDetail);
		lightPPRTParameter = new PixelPerfectRTParameter(cameraViewportUnitsCeiled, _initialSampleDetail * (int)iRenderSettings.lightResample);
		obstacleLightPPRTParameter = new PixelPerfectRTParameter(lightPPRTParameter.units, Mathf.Clamp(_initialSampleDetail, 2, int.MaxValue));
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
		       this.occlusionPPRTParameter == iOperation.occlusionPPRTParameter &&
		       this.fovPPRTParameter == iOperation.fovPPRTParameter &&
		       this.lightPPRTParameter == iOperation.lightPPRTParameter &&
		       this.obstacleLightPPRTParameter == iOperation.obstacleLightPPRTParameter;
	}
}