using UnityEngine;

/// <summary>
/// A pixel perfect render texture, designed so that we can have post processing effects which don't require
/// huge textures and have a size independent of the screen size / resolution and which avoid artifacts that could occur
/// while things are rotating / in motion.
/// </summary>
public class PixelPerfectRT
{
	private PixelPerfectRTParameter mPPRTParameter;
	private RenderTexture mRenderTexture;

	public PixelPerfectRT(PixelPerfectRTParameter iPprtParameter)
	{
		Update(iPprtParameter);
	}

	public RenderTexture renderTexture => mRenderTexture;

	public PixelPerfectRTParameter parameter => mPPRTParameter;

	public Vector2 renderPosition { get; set; }

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
	/// Gets the vector which can transform a vector in this PPRT's normalized space (bottom left (0,0), top right (1,1))
	/// to the camera's normalized viewport space (bottom left (0,0), top right (1,1)), using
	/// the camera's transform position as the position.
	///
	/// Transform can be applied by the following formula:
	/// dest = (src.xy - 0.5 + transform.xy) * transform.zw + 0.5
	/// </summary>
	/// <param name="iCamera">target camera</param>
	/// <returns>vector for performing the transformation, x and y hold the position offset, z and w hold the
	/// x and y scale difference, respectively</returns>
	public Vector4 GetTransformation(Camera iCamera)
	{
		Vector2 _cameraUnits = iCamera.ViewportToWorldPoint(Vector3.one) - iCamera.ViewportToWorldPoint(Vector3.zero);
		Vector2 _cameraPosition = iCamera.transform.position;

		return GetTransformation(_cameraUnits, _cameraPosition);
	}

	/// <summary>
	/// Gets the vector which can transform a vector in this PPRT's normalized space (bottom left (0,0), top right (1,1))
	/// to the pprt's normalized space (bottom left (0,0), top right (1,1)), using
	/// the target's renderPosition as the position.
	///
	/// Transform can be applied by the following formula:
	/// dest = (src.xy - 0.5 + transform.xy) * transform.zw + 0.5
	/// </summary>
	/// <param name="iTargetPerfectRT">target PPRT</param>
	/// <returns>vector for performing the transformation, x and y hold the position offset, z and w hold the
	/// x and y scale difference, respectively</returns>
	public Vector4 GetTransformation(PixelPerfectRT iTargetPerfectRT)
	{
		Vector2 _pprtUnits = iTargetPerfectRT.mPPRTParameter.units;
		Vector2 _pprtPosition = iTargetPerfectRT.renderPosition;

		return GetTransformation(_pprtUnits, _pprtPosition);
	}

	/// <summary>
	/// Gets the vector which can transform a vector in this PPRT's normalized space (bottom left (0,0), top right (1,1))
	/// to the pprt's normalized space (bottom left (0,0), top right (1,1)), using
	/// the specified position.
	///
	/// Transform can be applied by the following formula:
	/// dest = (src.xy - 0.5 + transform.xy) * transform.zw + 0.5
	/// </summary>
	/// <param name="iTargetPerfectRT">target PPRT</param>
	/// <param name="iOverridePosition">position to use for calculating the transformation</param>
	/// <returns>vector for performing the transformation, x and y hold the position offset, z and w hold the
	/// x and y scale difference, respectively</returns>
	public Vector4 GetTransformation(PixelPerfectRT iTargetPerfectRT, Vector2 iOverridePosition)
	{
		Vector2 _pprtUnits = iTargetPerfectRT.mPPRTParameter.units;
		Vector2 _pprtPosition = iOverridePosition;

		return GetTransformation(_pprtUnits, _pprtPosition);
	}

	/// <summary>
	/// Transforms the specified screen point to a point in this PPRT's normalized coordinate space
	/// (bottom left (0,0), top right (1,1))
	/// </summary>
	/// <param name="camera">camera whose screen coordinates are being used</param>
	/// <param name="screenPoint">point to transform</param>
	/// <returns>coordinates in this PPRT's normalized coordinate space</returns>
	public Vector2 ScreenToNormalTextureCoordinates(Camera camera, Vector2 screenPoint)
	{
		Vector4 transform = GetTransformation(camera);

		Vector2 final = camera.ScreenToViewportPoint(screenPoint);
		final.x -= 0.5f;
		final.y -= 0.5f;
		final.x += transform.x;
		final.y += transform.y;
		final.x *= transform.z;
		final.y *= transform.w;
		final.x += 0.5f;
		final.y += 0.5f;

		return final;
	}

	private Vector4 GetTransformation(Vector2 iTargetUnits, Vector2 iTargetPosition)
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

	/// <summary>
	/// Transforms the pixel perfect RT to fit the destination and blits to the destination.
	/// </summary>
	/// <param name="source">source PPRT</param>
	/// <param name="destination">destination PPRT</param>
	/// <param name="materialContainer">container to get the PPRT transform material from to perform the transformation</param>
	public static void Transform(
		PixelPerfectRT source,
		PixelPerfectRT destination,
		MaterialContainer materialContainer)
	{
		destination.renderPosition = source.renderPosition;

		materialContainer.PPRTTransformMaterial.SetVector("_Transform", source.GetTransformation(destination));
		materialContainer.PPRTTransformMaterial.SetTexture("_SourceTex", source.renderTexture);

		Graphics.Blit(source.renderTexture, destination.renderTexture, materialContainer.PPRTTransformMaterial);
	}

	/// <summary>
	/// Transforms the pixel perfect RT to fit the destination camera render texture and blits to the destination.
	/// Uses the camera to figure out the needed parameters to perform the transformation
	/// </summary>
	/// <param name="source">source PPRT</param>
	/// <param name="destination">destination camera render texture</param>
	/// <param name="destinationCamera">camera to use to determine the transformation properties</param>
	/// <param name="materialContainer">container to get the PPRT transform material from to perform the transformation</param>
	public static void Transform(
		PixelPerfectRT source,
		RenderTexture destination,
		Camera destinationCamera,
		MaterialContainer materialContainer)
	{
		materialContainer.PPRTTransformMaterial.SetVector("_Transform", source.GetTransformation(destinationCamera));
		materialContainer.PPRTTransformMaterial.SetTexture("_SourceTex", source.renderTexture);

		Graphics.Blit(source.renderTexture, destination, materialContainer.PPRTTransformMaterial);
	}
}