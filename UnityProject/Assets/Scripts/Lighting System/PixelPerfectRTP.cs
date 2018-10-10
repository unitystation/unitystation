using UnityEngine;

public class PixelPerfectRTP
{
	private readonly PixelPerfectRTParameter mPPRTParameter;
	private readonly RenderTexture mRenderTexture;
	private readonly Vector2 mRenderPosition;

	public PixelPerfectRTP(PixelPerfectRTParameter iPPRTParameter, RenderTexture iRenderTexture, Vector2 iRenderPosition)
	{
		mPPRTParameter = iPPRTParameter;
		mRenderTexture = iRenderTexture;
		mRenderPosition = iRenderPosition;
	}

	public RenderTexture renderTexture => mRenderTexture;

	/// <summary>
	/// Returns transformation that can be used to offset current PPRT from renderer.
	/// </summary>
	public Vector4 GetTransformation(Transform iAgainstTransform)
	{
		Vector2Int _units = mPPRTParameter.units;
		Vector2 _matchUnits = mPPRTParameter.matchUnits;

		// Correct for odd pixel size.
		// Note: In case when occlusionPixelsPerUnit is set to and odd value and one of unit dimensions
		// is even, we need to correct by subtracting by Unit in viewport divided by occlusionPixelsPerUnit divided by half.
		Vector2 _oddPixelCorrection = Vector2.zero;

		if (mPPRTParameter.pixelPerUnit % 2 != 0)
		{
			if (_units.x % 2 == 0)
			{
				_oddPixelCorrection.x = -((1 / _matchUnits.x) / mPPRTParameter.pixelPerUnit) * 0.5f;
			}

			if (_units.y % 2 == 0)
			{
				_oddPixelCorrection.y = -((1 / _matchUnits.y) / mPPRTParameter.pixelPerUnit) * 0.5f;
			}
		}

		// Calculate transformation.
		Vector2 _scaleDifference = new Vector2(_matchUnits.x / (float)_units.x, _matchUnits.y / (float)_units.y);
		Vector2 _positionDifference = new Vector2(iAgainstTransform.position.x - mRenderPosition.x, iAgainstTransform.position.y - mRenderPosition.y);

		Vector4 _transformation = new Vector4(
			(_positionDifference.x / _matchUnits.x) + _oddPixelCorrection.x,  // Position Offset.
			(_positionDifference.y / _matchUnits.y) + _oddPixelCorrection.y,
			_scaleDifference.x,												  // Scale.
			_scaleDifference.y);

		return _transformation;
	}
}