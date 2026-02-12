using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Managers;
using US13.Systems.StatusesAndEffects;

namespace US13.Systems.Antagonists.Objectives
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/CrewHasStatusEffect")]
	public class CrewHasStatusEffect : Objective
	{
		public bool CountDeadPlayersAsStatusEffectHavers = true;
		public StatusEffect StatusEffectToCheck;

		[Range(0f,1f)]
		public float PercentageOfCrewRequired = 0.5f;

		protected override void Setup()
		{
			// Nothing needs to be done here.
		}

		protected override bool CheckCompletion()
		{
			var allPlayers = PlayerList.Instance.InGamePlayers;
			if (allPlayers == null || allPlayers.Count == 0)
			{
				Loggy.Error("[Objective/CrewHasStatusEffect] - No in-game players found! Failing objective.");
				return false;
			}

			var countedPlayersWithStatusEffect = 0;
			foreach (var player in allPlayers)
			{
				if (HasStatusEffect(player)) countedPlayersWithStatusEffect++;
			}
			var requiredCount = Mathf.CeilToInt(PercentageOfCrewRequired * allPlayers.Count);
			Chat.AddGameWideSystemMsgToChat($"{countedPlayersWithStatusEffect} out of {allPlayers.Count} players " +
			                                $"have the {StatusEffectToCheck.name} status effect. {requiredCount} required to complete objective.");
			return countedPlayersWithStatusEffect >= requiredCount;
		}

		private bool HasStatusEffect(PlayerInfo player)
		{
			if (player == null || player.Mind == null || player.Mind.Body == null || player.Mind.Body.StatusEffectManager == null)
			{
				return false; // Skip null players or players without a body.
			}
			if (player.Mind.Body.IsDeadOrGhost && CountDeadPlayersAsStatusEffectHavers)
			{
				return true; // Count dead players as a "yes" in the final score.
			}

			return player.Mind.Body.StatusEffectManager.HasStatusByName(StatusEffectToCheck.name);
		}
	}
}