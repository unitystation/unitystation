using GameModes;
using Systems.Antagonists.Antags;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Special/BloodBrotherMainObjective")]
	public class BloodBrotherMainObjective : Objective
	{
		protected override void Setup()
		{
			// PISS
		}

		protected override bool CheckCompletion()
		{
			return BloodBrothers.AreBrothersAlive();
		}
	}
}