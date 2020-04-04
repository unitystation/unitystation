using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chemistry
{
	public static class ChemistryUtils
	{
		/// <summary>
		/// Returns rough estimation of reagent container fill (empty, almost full, etc)
		/// </summary>
		/// <param name="fillPercent">The fill percent from [0, 1]</param>
		/// <returns>Rough estimation as word</returns>
		public static string GetFillDescription(float fillPercent)
		{
			if (fillPercent <= 0f)
				return "empty";
			else if (fillPercent <= 0.25f)
				return "almost empty";
			else if (fillPercent <= 0.75f)
				return "half empty";
			else if (fillPercent <= 1f)
				return "almost full";
			else
				return "full";
		}

		public static string GetMixStateDescription(ReagentMix mix)
		{
			var mixState = mix.MixState;

			switch (mixState)
			{
				case ReagentState.Liquid:
					return "liquid";
				case ReagentState.Solid:
					return "powder";
				case ReagentState.Gas:
					return "gas";
				default:
					return "substance";
			}
		}

	}

}
