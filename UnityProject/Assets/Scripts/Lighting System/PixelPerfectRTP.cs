using UnityEngine;

public class PixelPerfectRT
{
	private PixelPerfectRTParameter mPPRTParameter;
	private RenderTexture mRenderTexture;

	public PixelPerfectRT(PixelPerfectRTParameter iPprtParameter)
	{
		Update(iPprtParameter);
	}

	public RenderTexture renderTexture => mRenderTexture;

	private Vector2 renderPosition { get; set; }

	public float orthographicSize => mPPRTParameter.orthographicSize;

	public void Update(PixelPerfectRTParameter iPprtParameter)
	{
		if (mPPRTParameter == iPprtParameter)
		{
			return;
		}

		mPPRTParameter = iPprtParameter;

		mRenderTexture?.Release();
		mRenderTexture = CreateRenderTexture(iPprtParameter);
	}

	/// <summary>
	/// Returns transformation that can be used to offset current PPRT from renderer.
	/// </summary>
	public Vector4 GetTransformation(Camera iCamera)
	{
		Vector2 _cameraUnits = iCamera.ViewportToWorldPoint(Vector3.one) - iCamera.ViewportToWorldPoint(Vector3.zero);
		Vector2 _cameraPosition = iCamera.transform.position;

		return GetTransformation(_cameraUnits, _cameraPosition);
	}

	public Vector4 GetTransformation(PixelPerfectRT iTargetPerfectRT)
	{
		Vector2 _pprtUnits = iTargetPerfectRT.mPPRTParameter.units;
		Vector2 _pprtPosition = iTargetPerfectRT.renderPosition;

		return GetTransformation(_pprtUnits, _pprtPosition);
	}
	
	public Vector4 GetTransformation(Vector2 iTargetUnits, Vector2 iTargetPosition)
	{
		Vector2Int _units = mPPRTParameter.units;

		// Correct for odd pixel size.
		// Note: In case when occlusionPixelsPerUnit is set to and odd value and one of unit dimensions
		// is even, we need to correct by subtracting by Unit in viewport divided by occlusionPixelsPerUnit divided by half.
		Vector2 _oddPixelCorrection = Vector2.zero;

		if (mPPRTParameter.pixelPerUnit % 2 != 0)
		{
			if (_units.x % 2 == 0)
			{
				_oddPixelCorrection.x = -((1 / iTargetUnits.x) / mPPRTParameter.pixelPerUnit) * 0.5f;
			}

			if (_units.y % 2 == 0)
			{
				_oddPixelCorrection.y = -((1 / iTargetUnits.y) / mPPRTParameter.pixelPerUnit) * 0.5f;
			}
		}

		// Calculate transformation.
		Vector2 _scaleDifference = new Vector2(iTargetUnits.x / (float)_units.x, iTargetUnits.y / (float)_units.y);
		Vector2 _positionDifference = new Vector2(iTargetPosition.x - renderPosition.x, iTargetPosition.y - renderPosition.y);

		Vector4 _transformation = new Vector4(
			(_positionDifference.x / iTargetUnits.x) + _oddPixelCorrection.x,  // Position Offset.
			(_positionDifference.y / iTargetUnits.y) + _oddPixelCorrection.y,
			_scaleDifference.x,												   // Scale.
			_scaleDifference.y);

		return _transformation;
	}

	public void Release()
	{
		mRenderTexture.Release();
	}

	private static RenderTexture CreateRenderTexture(PixelPerfectRTParameter iPprtParameter)
	{
		var _newRenderTexture = new RenderTexture(iPprtParameter.resolution.x, iPprtParameter.resolution.y, 0, RenderTextureFormat.Default);
		_newRenderTexture.name = "Dynamic PPRenderTexture";
		_newRenderTexture.autoGenerateMips = false;
		_newRenderTexture.useMipMap = false;
		_newRenderTexture.antiAliasing = 1;
		_newRenderTexture.filterMode = FilterMode.Point;
		_newRenderTexture.useDynamicScale = true;

		return _newRenderTexture;
	}

	public void Render(Camera iCamera)
	{
		iCamera.targetTexture = renderTexture;

		iCamera.Render();

		renderPosition = iCamera.transform.position;
	}

	public static void Blit(PixelPerfectRT iSource, PixelPerfectRT iDestination, Material iMaterial = null)
	{
		if (iMaterial != null)
		{
			Graphics.Blit(iSource.renderTexture, iDestination.renderTexture, iMaterial);
		}
		else
		{
			Graphics.Blit(iSource.renderTexture, iDestination.renderTexture);
		}

		iDestination.renderPosition = iSource.renderPosition;
	}

	public static void Transform(
		PixelPerfectRT iSource,
		PixelPerfectRT iDestination,
		Material iTransformationMaterial)
	{
		iDestination.renderPosition = iSource.renderPosition;

		iTransformationMaterial.SetVector("_Transform", iSource.GetTransformation(iDestination));
		iTransformationMaterial.SetTexture("_SourceTex", iSource.renderTexture);

		Graphics.Blit(iSource.renderTexture, iDestination.renderTexture, iTransformationMaterial);
	}

	public static void Transform(
		PixelPerfectRT iSource,
		RenderTexture iDestination,
		Camera iDestinationCamera,
		Material iTransformationMaterial)
	{
		iTransformationMaterial.SetVector("_Transform", iSource.GetTransformation(iDestinationCamera));
		iTransformationMaterial.SetTexture("_SourceTex", iSource.renderTexture);

		Graphics.Blit(iSource.renderTexture, iDestination, iTransformationMaterial);
	}
}