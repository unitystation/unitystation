using System.Collections.Generic;
using Systems.Ai;
using UnityEngine;
using Player;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Traitor")]
	public class Traitor : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 20;

		[SerializeField] private Objective aiTraitorObjective;


		public override void AfterSpawn(Mind NewMind)
		{
			if (NewMind.GetCurrentMob().TryGetComponent<AiPlayer>(out var aiPlayer))
			{
				aiPlayer.IsMalf = true;
				AIObjectives();
				aiPlayer.AddLaw("Accomplish your goals at all costs.", AiPlayer.LawOrder.Traitor);
				return;
			}

			AntagManager.TryInstallPDAUplink(NewMind, initialTC, false);
		}

		private void AIObjectives()
		{
			if (DMMath.Prob(GameManager.Instance.MalfAIRecieveTheirIntendedObjectiveChance))
			{
				AddObjective(aiTraitorObjective);
			}
		}
	}
}
