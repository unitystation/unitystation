using System;
using UnityEngine;

namespace US13.Core.Camera
{
	[Flags]
	public enum ColourBlindMode
	{
		None   = 0,
		Tritan = 1 << 0,
		Protan = 1 << 1,
		Deuntan = 1 << 2
	}

	public class ColourblindEmulation : CameraEffectBase
	{
		public Material material;

		[SerializeField]
		[NaughtyAttributes.EnumFlags]
		private ColourBlindMode CurrentColourMode;

		public void SetColourMode(ColourBlindMode inColourMode)
		{
			CurrentColourMode = inColourMode;

			enabled = CurrentColourMode != ColourBlindMode.None;
		}

		public override Material GetMaterial()
		{
			if (CurrentColourMode == ColourBlindMode.None) return null;

			Material mat = GetInstance(material);

			if (mat == null) return null;

			mat.SetInt(
				"_TRITAN",
				CurrentColourMode.HasFlag(ColourBlindMode.Tritan) ? 1 : 0
			);

			mat.SetInt(
				"_PROTAN",
				CurrentColourMode.HasFlag(ColourBlindMode.Protan) ? 1 : 0
			);

			mat.SetInt(
				"_DEUNTAN",
				CurrentColourMode.HasFlag(ColourBlindMode.Deuntan) ? 1 : 0
			);

			return mat;
		}
	}
}