using System;
using UnityEngine;

[Serializable]
public class FovRenderSettings
{
	[Range(0, 4)]
	public int maskDownscaling = 0;

	public float maskAlpha = 1;

	[Range(0, 0.01f)]
	public float maskCorrection = 0.01f;
	public int blurIterations;
	public float blurInterpolation;
	public int pixelateSize = 5;
}