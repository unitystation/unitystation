using System;
using UnityEngine;

[Serializable]
public class MaterialContainer
{
	public Material fovBlurMaterial;
	public Material blitMaterial;
	public Material lightBlurMaterial;
	public Material lightWallBlurMaterial;

	/// <summary>
	/// Material used for calculating the final occlusion mask, including floor and wall occlusion
	/// </summary>
	public Material fovMaterial;

	/// <summary>
	/// Material used for calculating floor-only occlusion
	/// </summary>
	public Material floorFovMaterial;

	public Material PPRTTransformMaterial;
	public Shader OcclusionMaskShader;
}