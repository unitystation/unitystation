using UnityEngine;

namespace US13.Core.Camera
{
	public class BlurryVision : CameraEffectBase
	{
		public Material material;

		[SerializeField]
		private int _BlurryStrength = 20;

		private bool HasEyeCorrection = false;

		public void SetHasEyeCorrection(bool inState)
		{
			HasEyeCorrection = inState;
			enabled = !HasEyeCorrection && _BlurryStrength > 0;
		}

		public void SetBlurStrength(int inStrength)
		{
			if (inStrength <= 0)
			{
				_BlurryStrength = 1;
			}
			else
			{
				_BlurryStrength = Mathf.Clamp(inStrength, 1, 30);
				enabled = !HasEyeCorrection;
			}
		}

		public override Material GetMaterial()
		{
			if (HasEyeCorrection || _BlurryStrength <= 0) return null;

			Material mat = GetInstance(material);

			if (mat == null) return null;

			mat.SetInt("_BlurryStrength", _BlurryStrength);

			return mat;
		}
	}
}