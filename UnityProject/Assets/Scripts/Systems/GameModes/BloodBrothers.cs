using System;
using System.Linq;
using Antagonists;
using Systems.Antagonists.Antags;
using Systems.Explosions;
using UnityEngine;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/BloodBrothers")]
	public class BloodBrothers : GameMode
	{
		public override void EndRoundReport()
		{
			base.EndRoundReport();
			if (AreBrothersAlive() && BrothersEarnedTheirFreedom())
			{
				Chat.AddGameWideSystemMsgToChat("<color=green><size+=35>The Blood Brothers have earned their freedom.");
			}
			else
			{
				Chat.AddGameWideSystemMsgToChat("<color=red><size+=35>The Blood Brothers have failed to earn their freedom.");
				OnBrotherDeath();
			}
		}

		public static void OnBrotherDeath()
		{
			foreach (var possibleBrother in AntagManager.Instance.ActiveAntags)
			{
				if (possibleBrother.Antagonist is not BloodBrother) continue;
				Chat.AddExamineMsg(possibleBrother.Owner.Body.gameObject,
					"You feel your fellow brother part ways with their body.. And you follow them.");
				possibleBrother.Owner.CurrentPlayScript.playerHealth.Death();
				if (DMMath.Prob(15))
				{
					Explosion.StartExplosion(possibleBrother.Owner.Body.gameObject.AssumedWorldPosServer().CutToInt(), 750);
				}
			}
		}

		public static bool AreBrothersAlive()
		{
			foreach (var possibleBrother in AntagManager.Instance.ActiveAntags)
			{
				if (possibleBrother.Antagonist is not BloodBrother) continue;
				if (possibleBrother.Owner.CurrentPlayScript.playerHealth.IsDead == false) continue;
				return false;
			}
			return true;
		}

		private bool BrothersEarnedTheirFreedom()
		{
			var totalNumberOfObjectives = 0;
			var totalNumberOfObjectivesCompleted = 0;

			foreach (var brother in AntagManager.Instance.ActiveAntags)
			{
				if (brother.Antagonist is not BloodBrother) continue;
				totalNumberOfObjectives += brother.Objectives.Count();
			}

			foreach (var brother in AntagManager.Instance.ActiveAntags)
			{
				if (brother.Antagonist is not BloodBrother) continue;
				foreach (var brotherObjective in brother.Objectives)
				{
					if (brotherObjective.IsComplete()) totalNumberOfObjectivesCompleted += 1;
				}
			}

			return totalNumberOfObjectivesCompleted / totalNumberOfObjectives > 0.7;
		}
	}
}