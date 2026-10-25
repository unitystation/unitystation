using NaughtyAttributes;
using UnityEngine;

namespace US13.Core.Camera
{
	public class GreyscaleCamera : CameraEffectBase
	{
		public Material material;
		[Range(0f, 1.0f)] public float amount;
		public Color tint = new Color(1, 1, 1, 1);

		public void Desaturate(float greyAmount) => amount = greyAmount;
		public void TintVision(Color toTint) => tint = toTint;

		[Button("Debug")]
		public void DebugRandomValue()
		{
			Desaturate(Random.Range(0, 1));
		}

		public override Material GetMaterial()
		{
			var mat = GetInstance(material);
			if (mat == null) return null;
			mat.SetColor("_Color", tint);
			mat.SetFloat("_EffectAmount", amount);
			return mat;
		}
	}
}