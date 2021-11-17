using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AddressableReferences;
using Audio.Containers;
using Grpc.Core;
using Managers;
using Mirror;
using ScriptableObjects.TimedGameEvents;
using UnityEngine;

namespace Objects.Other
{
	public class PineTreeXmas : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField] private SpriteDataSO xmasSpriteSO;
		[SerializeField] private TimedGameEventSO eventData;
		[SerializeField] private GameObject giftObject;
		[SerializeField] private AddressableAudioSource ambientReminder;

		[SyncVar] private List<string> giftedPlayers = new List<string>();
		private bool canPickUpGifts;

		private SpriteHandler spriteHandler;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			if (TimedEventsManager.Instance.ActiveEvents.Contains(eventData))
			{
				canPickUpGifts = true;
				spriteHandler.SetSpriteSO(xmasSpriteSO);
				PlayEasterEgg();
			}
		}

		private async void PlayEasterEgg()
		{
			await Task.Delay(60 * 1000); // millaseconds
			if (DMMath.Prob(10))
			{
				_ = SoundManager.PlayNetworkedAtPosAsync(ambientReminder, gameObject.WorldPosServer());
			}
			PlayEasterEgg();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (canPickUpGifts == false) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (giftedPlayers.Contains(interaction.PerformerPlayerScript.connectedPlayer.UserId))
			{
				Chat.AddExamineMsg(interaction.Performer, "You already received a gift!");
				return false;
			}
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Inventory.ServerSpawnPrefab(giftObject, interaction.HandSlot);
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You pick up a gift with your name on it.",
				$"{interaction.PerformerPlayerScript.visibleName} picks up a gift with {interaction.PerformerPlayerScript.characterSettings.TheirPronoun(interaction.PerformerPlayerScript)} name on it.");
			giftedPlayers.Add(interaction.PerformerPlayerScript.connectedPlayer.UserId);
		}
	}
}

