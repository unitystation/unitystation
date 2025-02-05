using System.Linq;
using GameModes;
using Systems.Antagonists.Antags;
using Systems.Explosions;
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
			return AreBrothersAlive();
		}

		public override string GetCompleteText(bool RichText)
		{
			return CheckFreedomStatus(RichText);

			//if (BrothersEarnedTheirFreedom() == false) OnBrotherDeath();
		}

		private static string CheckFreedomStatus(bool RichText)
		{
			if (AreBrothersAlive() && BrothersEarnedTheirFreedom())
			{
				return "<color=green><size=+35>The Blood Brothers have earned their freedom.</size></color>";
			}
			else
			{
				return "<color=red><size=+35>The Blood Brothers have failed to earn their freedom.</size></color>";
			}
		}

		public override void OnRoundEnd()
		{
			if (BrothersEarnedTheirFreedom() == false) OnBrotherDeath(true);
		}

		public void OnBrotherDeathNotEnd()
		{
			OnBrotherDeath(false);
		}

		public void OnBrotherDeath(bool Isending)
		{
			foreach (var possibleBrother in AntagManager.Instance.ActiveAntags)
			{
				if (possibleBrother.Antagonist is not BloodBrother) continue;
				Chat.AddExamineMsg(possibleBrother.Owner.Body.gameObject,
					"You feel your fellow brother part ways with their body.. And you follow them.");
				// We do this to avoid a stack overflow.
				if (possibleBrother.Owner.CurrentPlayScript.IsDeadOrGhost == false) possibleBrother.Owner.CurrentPlayScript.playerHealth.Death(false);
				if (DMMath.Prob(15))
				{
					Explosion.StartExplosion(possibleBrother.Owner.Body.gameObject.AssumedWorldPosServer().CutToInt(), 750, stunNearbyPlayers: true);
				}
			}

			if (Isending == false)
			{
				if (GameManager.Instance.CurrentRoundState != RoundState.Started) return;
				if (GameManager.Instance.GameMode is BloodBrothers)
				{
					GameManager.Instance.EndRound(GameManager.RoundID);
				}
			}
		}

		public static bool AreBrothersAlive()
		{
			foreach (var possibleBrother in AntagManager.Instance.ActiveAntags)
			{
				if (possibleBrother.Antagonist is not BloodBrother) continue;
				if (possibleBrother?.Owner?.CurrentPlayScript?.playerHealth?.IsDead == false) continue;
				return false;
			}
			return true;
		}

		private static bool BrothersEarnedTheirFreedom()
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

			return (totalNumberOfObjectivesCompleted / (float) totalNumberOfObjectives) > 0.7f;
		}



	}
}