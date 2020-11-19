using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Antagonists;

namespace InGameEvents
{
	public class EventSummonGuns : EventGiveGuns
	{
		[SerializeField]
		private string globalSound = default;

		[Tooltip("Set the percent chance a player will become an antagonist with a survival/steal guns objective.")]
		[SerializeField, Range(0, 100)]
		private int antagChance = 25;

		[Tooltip("The antagonist to spawn (survivor).")]
		[SerializeField, ShowIf(nameof(WillCreateAntags))]
		private Antagonist survivorAntag;

		[Tooltip("The unique objective to give to each survivor.")]
		[SerializeField, ShowIf(nameof(WillCreateAntags))]
		private Objective objective;

		private bool WillCreateAntags => antagChance > 0;

		public override void OnEventStart()
		{
			SoundManager.PlayNetworked(globalSound);

			survivorAntag.AddObjective(objective);
			SpawnGuns();
			survivorAntag.RemoveObjective(objective); // remove lest we reuse survivor antag for other events
		}

		protected override void HandlePlayer(ConnectedPlayer player)
		{
			GiveGunToPlayer(player);

			if (Random.Range(0, 100) < antagChance && player.Script.mind.IsAntag == false)
			{
				SetAsAntagSurvivor(player);
			}
		}

		private void SetAsAntagSurvivor(ConnectedPlayer player)
		{
			Chat.AddExamineMsgFromServer(player, "<color='red'><size=60>You are the survivalist!</size></color>");
			AntagManager.Instance.ServerFinishAntag(survivorAntag, player, player.GameObject);
		}
	}
}
