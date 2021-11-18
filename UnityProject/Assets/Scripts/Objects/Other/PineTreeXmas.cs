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
using UnityEngine.PlayerLoop;

namespace Objects.Other
{
	public class PineTreeXmas : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField] private SpriteDataSO xmasSpriteSO;
		[SerializeField] private TimedGameEventSO eventData;
		[SerializeField] private GameObject giftObject;
		[SerializeField] private AddressableAudioSource ambientReminder;

		private bool hasClicked = false;

		private List<string> giftedPlayers = new List<string>();
		private bool canPickUpGifts;

		private SpriteHandler spriteHandler;

		private void Start()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			if (TimedEventsManager.Instance.ActiveEvents.Contains(eventData))
			{
				canPickUpGifts = true;
				spriteHandler.SetSpriteSO(xmasSpriteSO);
				UpdateManager.Add(PlayEasterEgg, 60);
			}
		}

		[ContextMenu("Play the reminder of our finite time")]
		private void PlayEasterEgg()
		{
			if(gameObject == null) return;

			if (DMMath.Prob(10))
			{
				_ = SoundManager.PlayNetworkedAtPosAsync(ambientReminder, gameObject.WorldPosServer());
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (canPickUpGifts == false) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandSlot.IsOccupied) return false;
			if (hasClicked)
			{
				Chat.AddExamineMsg(interaction.Performer, "You already received a gift!", side);
				return false;
			}
			if(side == NetworkSide.Client) hasClicked = true;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(giftedPlayers.Contains(interaction.PerformerPlayerScript.connectedPlayer.UserId)) return;
			Inventory.ServerSpawnPrefab(giftObject, interaction.HandSlot, ReplacementStrategy.DropOther);
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You pick up a gift with your name on it.",
				$"{interaction.PerformerPlayerScript.visibleName} picks up a gift with {interaction.PerformerPlayerScript.characterSettings.TheirPronoun(interaction.PerformerPlayerScript)} name on it.");
			giftedPlayers.Add(interaction.PerformerPlayerScript.connectedPlayer.UserId);
		}
	}
}

