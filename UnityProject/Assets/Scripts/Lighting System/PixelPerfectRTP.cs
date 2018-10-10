using UnityEngine;

public class PixelPerfectRTP
{
	private PixelPerfectRTParameter mPPRTParameter;
	private RenderTexture mRenderTexture;
	private readonly Vector2 mRenderPosition;

	public PixelPerfectRTP(PixelPerfectRTParameter iPPRTParameter, RenderTexture iRenderTexture, Vector2 iRenderPosition)
	{
		mPPRTParameter = iPPRTParameter;
		mRenderTexture = iRenderTexture;
		mRenderPosition = iRenderPosition;
	}

	public RenderTexture renderTexture => mRenderTexture;

	public Vector4 GetOffset(Transform iAgainstTransform)
	{
		Vector2 _positionDifference = new Vector2(iAgainstTransform.position.x - mRenderPosition.x, iAgainstTransform.position.y - mRenderPosition.y);

		Vector2Int _units = mPPRTParameter.units;
		Vector2 _matchAgainstUnits = mPPRTParameter.matchAgainstUnits;

		Vector2 _difference = new Vector2(_matchAgainstUnits.x / (float)_units.x, _matchAgainstUnits.y / (float)_units.y);

		// Correct for odd pixel size.
		// Note: In case when occlusionPixelsPerUnit is set to and odd value and one of unit dimensions
		// is even, we need to correct by subtracting by Unit in viewport divided by occlusionPixelsPerUnit divided by half.
		Vector2 _oddPixelCorrection = Vector2.zero;

		if (mPPRTParameter.pixelPerUnit % 2 != 0)
		{
			if (_units.x % 2 == 0)
			{
				_oddPixelCorrection.x = -((1 / _matchAgainstUnits.x) / mPPRTParameter.pixelPerUnit) * 0.5f;
			}

			if (_units.y % 2 == 0)
			{
				_oddPixelCorrection.y = -((1 / _matchAgainstUnits.y) / mPPRTParameter.pixelPerUnit) * 0.5f;
			}
		}

		Vector4 _viewportOffsetScale = new Vector4(
			_positionDifference.x / _matchAgainstUnits.x + _oddPixelCorrection.x,
			_positionDifference.y / _matchAgainstUnits.y + _oddPixelCorrection.y,
			_difference.x,
			_difference.y);

		return _viewportOffsetScale;
	}
}