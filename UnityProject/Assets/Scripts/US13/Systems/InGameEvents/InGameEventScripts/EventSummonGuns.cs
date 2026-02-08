using NaughtyAttributes;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Managers;
using US13.Player;
using US13.Strings;
using US13.Systems.Antagonists;

namespace US13.Systems.InGameEvents.InGameEventScripts
{
	public class EventSummonGuns : EventGiveGuns
	{
		[SerializeField] private AddressableAudioSource globalSound = null;

		[Tooltip("Set the percent chance a player will become an antagonist with a survival/steal guns objective.")]
		[SerializeField, Range(0, 100)]
		private int antagChance = 25;

		[Tooltip("The antagonist to spawn (survivor).")]
		[SerializeField, ShowIf(nameof(WillCreateAntags))]
		private Antagonist survivorAntag = default;

		[Tooltip("The unique objective to give to each survivor.")]
		[SerializeField, ShowIf(nameof(WillCreateAntags))]
		private Objective objective = default;

		private bool WillCreateAntags => antagChance > 0;

		public override void OnEventStart()
		{
			_ = SoundManager.PlayNetworked(globalSound);

			survivorAntag.AddObjective(objective);
			SpawnGuns();
			survivorAntag.RemoveObjective(objective); // remove lest we reuse survivor antag for other events
		}

		protected override void HandlePlayer(Mind player)
		{
			GiveGunToPlayer(player);

			if (Random.Range(0, 100) < antagChance && player.IsAntag == false)
			{
				SetAsAntagSurvivor(player);
			}
		}

		private void SetAsAntagSurvivor(Mind player)
		{
			Chat.AddExamineMsgFromServer(player.gameObject, $"<color=red><size={ChatTemplates.VeryLargeText}>You are the survivalist!</size></color>");
			AntagManager.Instance.ServerFinishAntag(survivorAntag, player);
		}
	}
}
