using UnityEngine;
using US13.Managers;
using US13.Player;
using US13.Systems.Ai;
using Util;

namespace US13.Systems.Antagonists.Antags
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
