using UnityEngine;

namespace US13.Core.Camera
{
	[ExecuteAlways]
	public class NoirCamera : CameraEffectBase
	{
		private static readonly int radius = Shader.PropertyToID("_LensRadius");
		private static readonly int speed = Shader.PropertyToID("_GrainSpeed");

		[Header("Noir Effect")]
		public Shader shader;

		[Range(0.5f, 2f)]
		public float lensRadius = 0.5f;

		[Range(0f, 1f)]
		public float grainSpeed = 0.1f;

		private const float MAX_LENS_RADIUS = 4f;

		private Material runtimeMaterial;
		private bool lensRadiusMaxed;

		private void OnEnable()
		{
			EnsureShader();
		}

		private void OnValidate()
		{
			EnsureShader();
		}

		private void EnsureShader()
		{
			if (shader == null) shader = Shader.Find("Hidden/Noir Effect");
		}

		public override Material GetMaterial()
		{
			EnsureShader();

			if (shader == null) return null;

			if (runtimeMaterial == null)
			{
				runtimeMaterial = new Material(shader)
				{
					name = "Runtime Noir Effect Material",
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			Material mat = GetInstance(runtimeMaterial);

			if (mat == null) return null;

			mat.SetFloat(
				radius,
				lensRadiusMaxed ? MAX_LENS_RADIUS : lensRadius
			);

			mat.SetFloat(speed, grainSpeed);

			return mat;
		}

		public void HasMaxedLensRadius(bool set)
		{
			lensRadiusMaxed = set;
		}

		protected override void OnDestroy()
		{
			if (runtimeMaterial != null)
			{
				DestroyImmediate(runtimeMaterial);
				runtimeMaterial = null;
			}
		}
	}
}