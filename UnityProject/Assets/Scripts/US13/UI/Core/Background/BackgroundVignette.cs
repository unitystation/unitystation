using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Core.Background
{
	/// <summary>
	/// Drives the vignette of the menu overlay shader, optionally breathing the intensity in and out.
	/// </summary>
	public class BackgroundVignette : MonoBehaviour
	{
		private static readonly int IntensityProperty = Shader.PropertyToID("_VignetteIntensity");
		private static readonly int ColourProperty = Shader.PropertyToID("_VignetteColor");

		[SerializeField] private Graphic overlayGraphic = null;
		[SerializeField, Range(0f, 1f)] private float intensity = 0.5f;
		[SerializeField] private Color tint = Color.black;
		[SerializeField] private bool breathing = false;
		[SerializeField] private float breathingSpeed = 0.5f;
		[SerializeField, Range(0f, 1f)] private float breathingAmount = 0.15f;

		private float breathingTimer;

		private void OnEnable()
		{
			ApplyVignette(intensity);
		}

		public void SetIntensity(float value)
		{
			intensity = Mathf.Clamp01(value);
			ApplyVignette(intensity);
		}

		private void Update()
		{
			if (breathing == false) return;
			breathingTimer += Time.deltaTime * breathingSpeed;
			ApplyVignette(Mathf.Clamp01(intensity + Mathf.Sin(breathingTimer) * breathingAmount));
		}

		private void ApplyVignette(float amount)
		{
			if (overlayGraphic == null || overlayGraphic.material == null) return;
			overlayGraphic.material.SetFloat(IntensityProperty, amount);
			overlayGraphic.material.SetColor(ColourProperty, tint);
		}

		private void OnValidate()
		{
			ApplyVignette(intensity);
		}
	}
}
