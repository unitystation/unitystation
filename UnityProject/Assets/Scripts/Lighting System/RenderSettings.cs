using System;
using UnityEngine;

[Serializable]
public class RenderSettings
{

	[Tooltip("View override for quick overview of the process stages. For Debug purposes.")]
	public ViewMode viewMode;

	[Tooltip("Intensity of the mask alpha.")]
	public float ambient = 1;

	[Tooltip("Multiplication of lighting pass for quick adjustment.")]
	public float lightMultiplier;

	public float bloomSensitivity = 5;

	public float bloomAdd;

	[Tooltip("Multiplication of background pass for quick adjustment.")]
	public float backgroundMultiplier;

	[Tooltip("Layer Names for objects that occlude Light and View. This objects will be rendered in to R Channel mask that will be applied without blurring.")]
	public LayerMask occlusionLayers;

	[Tooltip("Layer Names for objects will be rendered as Light Sources.")]
	public LayerMask lightSourceLayers;

	[Tooltip("Layer Names for objects will be rendered as Background.")]
	public LayerMask backgroundLayers;

	[Tooltip("Number of passes for blur post-effect. Higher values will improve quality but has a negative performance impact. 1 is reasonable.")]
	public int fovBlurIterations;

	[Tooltip("Spread of blur post-effect that will be applied inside each pass")]
	public float fovBlurInterpolation;

	public float fovOcclusionSpread;

	public float fovHorizonSmooth;

	[Tooltip("Number of passes for blur post-effect. Higher values will improve quality but has a negative performance impact. 2 is reasonable.")]
	public int lightBlurIterations;

	[Tooltip("Spread of blur post-effect that will be applied inside each pass")]
	public float lightBlurInterpolation;

	[Tooltip("Number of passes for blur post-effect.")]
	public int occlusionBlurIterations;

	[Tooltip("Spread of blur post-effect that will be applied inside each pass. In this context it affects how deep light penetrates in to occluded objects. Must be balanced with occlusionMaskMultiplier and occlusionMaskLimit to control occlusion lighting.")]
	public float occlusionBlurInterpolation;

	public float occlusionMaskMultiplier;
	public float occlusionMaskLimit;

	[Tooltip("Scale of Occlusion light texture. Affected by lightTextureWidth. Occlusion light texture are quite small and mostly controlled to produce desired blur effect.")]
	public float occlusionLightTextureRescale = 0.25f;

	[Tooltip("Orthographic Size addition to Occlusion camera. Affects Extended texture size. Used to properly draw out of bounds light sources.")]
	public float maskCameraSizeAdd;

	[Tooltip("Used for occlusion texture only. 4 is a good balance..")]
	public int antiAliasing = 4;

	[NonSerialized]
	public Quality quality;

	private static int[] LightTextureResolutions = {300, 512, 700};

	private static float[] OcclusionUvAdjustments = {-0.007f, -0.004f, -0.004f};

	public float occlusionUvAdjustment
	{
		get
		{
			return OcclusionUvAdjustments[(int)quality];
		}
	}

	public int lightTextureWidth
	{
		get
		{
			return LightTextureResolutions[(int)quality];
		}
	}

	public enum ViewMode
	{
		Final,
		LightMix,
		LightLayerBlurred,
		LightLayer,
		WallLayer,
		FovObstacle,
		FovObstacleExtended,
		Background,
	};

	public enum Quality
	{
		Low,
		Mid,
		High,
	}
}