using System.Collections.Generic;
using Systems.Ai;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Traitor")]
	public class Traitor : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 20;

		[SerializeField] private Objective aiTraitorObjective;

		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}

		public override void AfterSpawn(ConnectedPlayer player)
		{
			if (player.GameObject.TryGetComponent<AiPlayer>(out var aiPlayer))
			{
				aiPlayer.IsMalf = true;
				AIObjectives();
				aiPlayer.AddLaw("Accomplish your goals at all costs.", AiPlayer.LawOrder.Traitor);
				return;
			}

			AntagManager.TryInstallPDAUplink(player, initialTC, false);
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
