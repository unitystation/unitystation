using Systems.Antagonists.Antags;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Special/BloodBrotherMainObjective")]
	public class BloodBrotherMainObjective : Objective
	{
		protected override void Setup() { }

		protected override bool CheckCompletion()
		{
			foreach (var possibleBrother in AntagManager.Instance.ActiveAntags)
			{
				if (possibleBrother.Antagonist is not BloodBrother) continue;
				if (possibleBrother.Owner.CurrentPlayScript.playerHealth.IsDead == false) continue;
				return false;
			}
			return true;
		}
	}
}