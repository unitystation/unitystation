using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Core.Background
{
	/// <summary>
	/// Drives the grain of the menu overlay shader. The shader animates the grain itself, so no per-frame work is needed.
	/// </summary>
	public class FilmGrainOverlay : MonoBehaviour
	{
		private static readonly int IntensityProperty = Shader.PropertyToID("_GrainIntensity");
		private static readonly int ScaleProperty = Shader.PropertyToID("_GrainScale");

		[SerializeField] private Graphic overlayGraphic = null;
		[SerializeField, Range(0f, 1f)] private float intensity = 0.05f;
		[SerializeField] private float scale = 900f;

		private void OnEnable()
		{
			ApplyGrain();
		}

		public void SetIntensity(float value)
		{
			intensity = Mathf.Clamp01(value);
			ApplyGrain();
		}

		private void ApplyGrain()
		{
			if (overlayGraphic == null || overlayGraphic.material == null) return;
			overlayGraphic.material.SetFloat(IntensityProperty, intensity);
			overlayGraphic.material.SetFloat(ScaleProperty, scale);
		}

		private void OnValidate()
		{
			ApplyGrain();
		}
	}
}
