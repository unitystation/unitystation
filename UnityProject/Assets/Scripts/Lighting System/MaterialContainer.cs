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
	public Material MaskMixerMaterial;

	public Shader OcclusionMaskShader;
}