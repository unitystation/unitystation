using System;
using UnityEngine;

[Serializable]
public class MaterialContainer
{
	public Material fovBlurMaterial;
	public Material blitMaterial;
	public Material lightBlurMaterial;
	public Material lightWallBlurMaterial;
	public Material fovMaterial;
	public Material fovWallMaterial;
	public Material MaskMixerMaterial;

	public Material PPRTTransformMaterial;

	public Shader OcclusionMaskShader;
}