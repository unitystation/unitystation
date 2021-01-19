//------------------------------------------------------------------------------
// Calculates diferent blend modes.
//
// TODO: Confirm and add blend modes https://en.wikipedia.org/wiki/Blend_modes
// [x] Normal			Need confirmation
// [x] Multiply			Need confirmation
// [x] Screen			Need confirmation
// [ ] Overlay
// [ ] Hard Light
// [ ] Soft Light
// [ ] Dodge
// [ ] Burn
// [ ] Divide
// [ ] Addition
// [ ] Subtract
// [ ] Difference
// [ ] Darken Only
// [ ] Lighten Only
//------------------------------------------------------------------------------
using System;
using UnityEngine;

namespace AssemblyCSharpEditor
{
	public class UPABlendModes
	{
		public static Texture2D Blend (Texture2D lowerLayer, float lowerOpacity, Texture2D upperLayer, float upperOpacity, UPALayer.BlendMode mode) {
			switch (mode) {
			case UPALayer.BlendMode.NORMAL:
				return Normal(lowerLayer, lowerOpacity, upperLayer, upperOpacity);
			case UPALayer.BlendMode.MULTIPLY:
				return Multiply(lowerLayer, lowerOpacity, upperLayer, upperOpacity);
			case UPALayer.BlendMode.SCREEN:
				return Screen(lowerLayer, lowerOpacity, upperLayer, upperOpacity);
			default:
				return Normal(lowerLayer, lowerOpacity, upperLayer, upperOpacity);
			}

		}

		private static Texture2D Normal(Texture2D lowerLayer, float lowerOpacity, Texture2D upperLayer, float upperOpacity) {
			Texture2D result = new Texture2D (lowerLayer.width, lowerLayer.height);
			result.filterMode = FilterMode.Point;
			for (int x = 0; x < lowerLayer.width; x++) {
				for (int y = 0; y < lowerLayer.height; y++) {
					Color lower = lowerLayer.GetPixel (x, y);
					Color upper = upperLayer.GetPixel (x, y);
					float srca = upper.a * upperOpacity;
					float lowa = lower.a * lowerOpacity;
					float outa = srca + lowa * (1 - srca);
					Color c = new Color ((upper.r * srca + lower.r * lowa * (1-srca)) / outa,
					                     (upper.g * srca + lower.g * lowa * (1-srca)) / outa,
					                     (upper.b * srca + lower.b * lowa * (1-srca)) / outa,
					                     outa);

					result.SetPixel (x, y, c);
				}
			}

			result.Apply ();
			return result;
		}

		private static Texture2D Multiply(Texture2D lowerLayer, float lowerOpacity, Texture2D upperLayer, float upperOpacity) {
			Texture2D result = new Texture2D (lowerLayer.width, lowerLayer.height);
			result.filterMode = FilterMode.Point;
			for (int x = 0; x < lowerLayer.width; x++) {
				for (int y = 0; y < lowerLayer.height; y++) {
					Color lower = lowerLayer.GetPixel (x, y);
					Color upper = upperLayer.GetPixel (x, y);
					float srca = upper.a * upperOpacity;
					float lowa = lower.a * lowerOpacity;

					Color c = Color.clear;
					if (srca == 0 && lowa != 0) {
						c = new Color(lower.r, lower.g, lower.b, lowa);
					} else if (lowa == 0 && srca != 0){
						c = new Color(upper.r, upper.g, upper.b, srca);
					} else if (lowa != 0 && srca != 0) {
						float outa = srca + lowa * (1 - srca);
						c = new Color ((upper.r * upper.a * lower.r * lower.a) / outa,
					                     (upper.g * upper.a * lower.g * lower.a) / outa,
					                     (upper.b * upper.a * lower.b * lower.a) / outa,
					                     outa);
					}

					result.SetPixel (x, y, c);
				}
			}
			
			result.Apply ();
			return result;
		}

		private static Texture2D Screen(Texture2D lowerLayer, float lowerOpacity, Texture2D upperLayer, float upperOpacity) {
			Texture2D result = new Texture2D (lowerLayer.width, lowerLayer.height);
			result.filterMode = FilterMode.Point;
			for (int x = 0; x < lowerLayer.width; x++) {
				for (int y = 0; y < lowerLayer.height; y++) {
					Color lower = lowerLayer.GetPixel (x, y);
					Color upper = upperLayer.GetPixel (x, y);
					float srca = upper.a * upperOpacity;
					float lowa = lower.a * lowerOpacity;
					Color c = Color.clear;
					if (srca == 0 && lowa != 0) {
						c = new Color(lower.r, lower.g, lower.b, lowa);
					} else if (lowa == 0 && srca != 0){
						c = new Color(upper.r, upper.g, upper.b, srca);
					} else if (lowa != 0 && srca != 0) {
						float outa = srca + lowa * (1 - srca);
						c = new Color ((1 - (1 - upper.r * srca) * (1 - lower.r * lowa)) / outa,
						               (1 - (1 - upper.g * srca) * (1 - lower.g * lowa)) / outa,
						               (1 - (1 - upper.b * srca) * (1 - lower.b * lowa)) / outa,
						               outa);
					}
					
					result.SetPixel (x, y, c);
				}
			}
			
			result.Apply ();
			return result;
		}

	}
}

