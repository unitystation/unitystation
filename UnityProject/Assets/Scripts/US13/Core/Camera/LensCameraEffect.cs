using UnityEngine;

namespace US13.Core.Camera
{
	public abstract class LensCameraEffect : CameraEffectBase
	{
		public Shader shader;
		[Range(0.5f, 2f)] public float lensRadius = 0.5f;

		protected const float MAX_LENS_RADIUS = 4f;
		private bool lensRadiusMaxed;

		public void HasMaxedLensRadius(bool set) => lensRadiusMaxed = set;

		public override Material GetMaterial()
		{
			var mat = GetInstance(shader);
			if (mat == null) return null;
			mat.SetFloat("_LensRadius", lensRadiusMaxed ? MAX_LENS_RADIUS : lensRadius);
			return mat;
		}
	}
}