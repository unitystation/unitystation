using System;
using Logs;
using UnityEngine;

public class HighCamera : MonoBehaviour
{

	public float _HueSpeed;
	public float _HueIntensity;
	public float _PlasmaSpeed;
	public float _PlasmaScale;
	public float _PlasmaBlend;
	public float _DistortionAmount;
	public float _DistortTimeSpeed;

	// Internal phase accumulators
	private float huePhase;
	private float plasmaPhase;
	private float distortPhase;

	private float targetDistortionAmount;

	public void SetStrength(float Strength)
	{
		Loggy.Error(Strength.ToString());

		if (Strength > 0.75f)
		{
			var CorrectedStrength = (Strength - 0.75f) * 4;

			//max =
			//_HueSpeed = 0.5
			//_HueIntensity = 1
			//_PlasmaSpeed = 1.25
			//_PlasmaScale = 0.5
			//_PlasmaBlend = 1
			//_DistortionAmount = 0.04
			//_DistortTimeSpeed = 1
			_HueSpeed = Mathf.Lerp(0.1f, 0.5f, CorrectedStrength);
			_HueIntensity = 1;
			_PlasmaSpeed = 1.25f;
			_PlasmaScale = 0.5f;
			_PlasmaBlend = Mathf.Lerp(0.295f,  1f, CorrectedStrength);
			targetDistortionAmount = Mathf.Lerp(0.02f,  0.04f, CorrectedStrength);
			_DistortTimeSpeed = Mathf.Lerp(0.1f,  1f, CorrectedStrength);
			this.enabled = true;
		}
		else if (Strength > 0.55f)
		{
			//mid =
			_HueSpeed = 0.1f;
			_HueIntensity = 1f;
			_PlasmaSpeed = 1.25f;
			_PlasmaScale = 0.5f;
			_PlasmaBlend = 0.295f;
			targetDistortionAmount = 0.02f;
			_DistortTimeSpeed = 0.1f;
			this.enabled = true;
		}
		else if (Strength > 0.35f)
		{
			var CorrectedStrength = (Strength - 0.35f) * 5f;

			_HueSpeed =  0.1f;
			_HueIntensity = Mathf.Lerp(0.25f, 1, CorrectedStrength);
			_PlasmaSpeed = Mathf.Lerp(1f, 1.25f, CorrectedStrength);
			_PlasmaScale = 0.5f;
			_PlasmaBlend = Mathf.Lerp(0.125f,  0.295f, CorrectedStrength);
			targetDistortionAmount = Mathf.Lerp(0f,  0.02f, CorrectedStrength);
			_DistortTimeSpeed = Mathf.Lerp(0.5f,  0.1f, CorrectedStrength);
			this.enabled = true;
		}
		else if (Strength > 0.10)
		{
			//low
			_HueSpeed = 0.1f;
			_HueIntensity = 0.25f;
			_PlasmaSpeed = 1f;
			_PlasmaScale = 0.5f;
			_PlasmaBlend = 0.125f;
			targetDistortionAmount = 0f;
			_DistortTimeSpeed = 0.5f;
			this.enabled = true;
		}
		else if (Strength > 0)
		{
			var CorrectedStrength = (Strength - 0) * 10f;

			_HueSpeed = Mathf.Lerp(0, 0.1f, CorrectedStrength);
			_HueIntensity = Mathf.Lerp(0f, 0.25f, CorrectedStrength);
			_PlasmaSpeed = Mathf.Lerp(0, 1f, CorrectedStrength);
			_PlasmaScale = 0.5f;
			_PlasmaBlend = Mathf.Lerp(0f,  0.125f, CorrectedStrength);
			targetDistortionAmount =0;
			_DistortTimeSpeed = Mathf.Lerp(0,  0.5f, CorrectedStrength);
			this.enabled = true;
		}
		else
		{
			this.enabled = false;
		}


	}

	public void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE ,UpdateMe);
	}

	public void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE ,UpdateMe);
	}


	void UpdateMe()
	{
		// Smooth distortion only
		_DistortionAmount = Mathf.Lerp(_DistortionAmount, targetDistortionAmount, Time.deltaTime*8);


		// Increment phases smoothly
		huePhase     += Time.deltaTime * _HueSpeed;
		plasmaPhase  += Time.deltaTime * _PlasmaSpeed;
		distortPhase += Time.deltaTime * _DistortTimeSpeed;

		// Wrap around (to avoid float overflow)
		if (huePhase > 100000) huePhase = 0;
		if (plasmaPhase > 100000) plasmaPhase = 0;
		if (distortPhase > 100000) distortPhase = 0;
	}

	public Material material;
	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		material.SetFloat("_HueSpeed", _HueSpeed);
		material.SetFloat("_HueIntensity", _HueIntensity);
		material.SetFloat("_PlasmaSpeed", _PlasmaSpeed);
		material.SetFloat("_PlasmaScale", _PlasmaScale);
		material.SetFloat("_PlasmaBlend", _PlasmaBlend);
		material.SetFloat("_DistortionAmount", _DistortionAmount);
		material.SetFloat("_DistortTimeSpeed", _DistortTimeSpeed);

		material.SetFloat("_HuePhase", huePhase);
		material.SetFloat("_PlasmaPhase", plasmaPhase);
		material.SetFloat("_DistortPhase", distortPhase);


		Graphics.Blit(source, destination, material);
	}
}
