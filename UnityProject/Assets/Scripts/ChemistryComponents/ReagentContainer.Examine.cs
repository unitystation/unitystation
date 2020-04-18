using Chemistry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chemistry.Components
{
	public partial class ReagentContainer : IExaminable
	{
		public enum ExamineAmountMode
		{
			APROXIMATE_AMOUNT, // player only can see that container is empty, half-empty or full
			EXACT_AMOUNT // player can see exact reagent mix ammount in units
		}

		public enum ExamineContentMode
		{
			NONE, // player can't see what reagent inside container
			COLOR_AND_STUCTURE // container is transparent and player can see reagent color and structure
		}

		[Header("Examine settings")]
		[Tooltip("Generate automatic examine message about contauner content?")]
		public bool UseStandardExamine = true;
		public ExamineAmountMode ExamineAmount = ExamineAmountMode.APROXIMATE_AMOUNT;
		public ExamineContentMode ExamineContent = ExamineContentMode.NONE;

		public string Examine(Vector3 worldPos = default)
		{
			if (!UseStandardExamine)
				return null;

			var fillPercent = GetFillPercent();
			if (fillPercent <= 0f)
				return "It's empty.";

			var fillDesc = ChemistryUtils.GetFillDescription(fillPercent);
			var stateDesc = ChemistryUtils.GetMixStateDescription(CurrentReagentMix);

			var color = CurrentReagentMix.MixColor;
			var colorDesc = TextUtils.ColorToString(color);
			var units = Mathf.RoundToInt(ReagentMixTotal);

			if (ExamineAmount == ExamineAmountMode.APROXIMATE_AMOUNT)
			{
				if (ExamineContent == ExamineContentMode.NONE)
				{
					return $"It's {fillDesc}";
				}
				else if (ExamineContent == ExamineContentMode.COLOR_AND_STUCTURE)
				{
					return $"It's {fillDesc}. There is {colorDesc} {stateDesc} inside";
				}
			}
			else if (ExamineAmount == ExamineAmountMode.EXACT_AMOUNT)
			{
				if (ExamineContent == ExamineContentMode.NONE)
				{
					return $"It's {fillDesc}. There are {units} units of something inside";
				}
				else if (ExamineContent == ExamineContentMode.COLOR_AND_STUCTURE)
				{
					return $"It's {fillDesc}. There are {units} units of {colorDesc} {stateDesc} inside";
				}
			}

			return null;
		}
	}
}