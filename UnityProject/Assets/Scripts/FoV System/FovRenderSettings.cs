using System;
using UnityEngine;

[Serializable]
public class FovRenderSettings
{
	[Tooltip("Intensity of the mask alpha.")]
	public float ambient = 1;

	public float lightMultiplier;

	[Tooltip("Number of passes for blur post-effect. Higher values will improve quality but has a negative performance impact. 2 is reasonable.")]
	public int fovBlurIterations;

	[Tooltip("Spread of blur post-effect that will be applied inside each pass")]
	public float fovBlurInterpolation;

	[Tooltip("Pixel size for Pixelate post-effect.")]
	public int pixelateSize = 5;

	[Tooltip("Layer Names for objects that must be un-obscured by FoV. This objects will be rendered in to R Channel mask that will be applied without blurring.")]
	public string[] unObscuredLayers;

	public int lightBlurIterations;

	public float lightBlurInterpolation;

	public int wallBlurIterations;

	public float wallBlurInterpolation;

	public float maskCameraSizeAdd;
	public float lightTextureRescale = 1;
}