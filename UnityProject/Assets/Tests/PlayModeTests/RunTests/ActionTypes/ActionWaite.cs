using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;


public partial class TestAction
{
	public bool ShowActionWaite => SpecifiedAction == ActionType.ActionWaite;

	[AllowNesting] [ShowIf(nameof(ShowActionWaite))]
	public ActionWaite DataActionWaite;

	[System.Serializable]
	public class ActionWaite
	{
		public bool WaitForFrame = false;
		public float WaitForSeconds = 0;

		public Preset SetPreset;

		public enum Preset
		{
			None,
			RunPressAndUnpress,
			InteractionCooldown,
			PickUpDelay
		}

		public bool Initiate(TestRunSO TestRunSO)
		{
			TestRunSO.BoolYieldInstruction = true;

			if (WaitForFrame)
			{
				TestRunSO.YieldInstruction = null;
			}

			if (WaitForSeconds != 0 || SetPreset != Preset.None)
			{
				switch (SetPreset)
				{
					case Preset.None:
						TestRunSO.YieldInstruction = WaitFor.Seconds(WaitForSeconds);
						break;
					case Preset.RunPressAndUnpress:
						TestRunSO.YieldInstruction = WaitFor.Seconds(0.3f);
						break;
					case Preset.InteractionCooldown:
						TestRunSO.YieldInstruction = WaitFor.Seconds(0.25f);
						break;
					case Preset.PickUpDelay:
						TestRunSO.YieldInstruction = WaitFor.Seconds(0.25f);
						break;

				}
			}

			return true;
		}
	}

	public bool InitiateActionWaite(TestRunSO TestRunSO)
	{
		return DataActionWaite.Initiate(TestRunSO);
	}
}