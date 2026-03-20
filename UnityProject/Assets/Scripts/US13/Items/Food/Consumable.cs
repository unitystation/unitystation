using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.HealthV2.Living.Surgery;
using US13.Player;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Items.Food
{
	/// <summary>
	/// Item that can be drinked or eaten by player
	/// Also supports force feeding other player
	/// </summary>
	public abstract class Consumable : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField] protected float consumeTime = 0.1f;

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null && interaction.Performer.GetCachedComponent<ConsumeFromFloor>() != null)
			{
				//If consume from floor just try to consume
				TryConsume(interaction.Performer);
				return;
			}

			if (gameObject.TryGetComponent<HandPreparable>(out var preparable))
			{
				if (preparable.IsPrepared == false)
				{
					Chat.AddExamineMsg(interaction.Performer, preparable.openingRequirementText);
					return;
				}
			}
			var targetPlayer = interaction.TargetObject.GetCachedComponent<PlayerScript>();
			if (targetPlayer == null) return;

			PlayerScript feeder = interaction.PerformerPlayerScript;
			var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();
			if (feederSlot.Item == null)
			{   //Already been eaten or the food is no longer in hand
				return;
			}

			PlayerScript eater = targetPlayer;
			TryConsume(feeder.gameObject, eater.gameObject);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.Intent != Intent.Help) return false;
			if (interaction.HandObject == null && interaction.Performer.GetCachedComponent<ConsumeFromFloor>() != null)
			{
				//Default check and allow any player if they have this script to do this
				if (DefaultWillInteract.Default(interaction, side, interaction.PerformerPlayerScript.PlayerType)) return true;
			}

			//this item shouldn't be a target
			if (Validations.IsTarget(gameObject, interaction)) return false;
			var Dissectible = interaction?.TargetObject.OrNull()?.GetComponent<Dissectible>();
			if (Dissectible != null)
			{
				if (Dissectible.GetBodyPartIsopen && Dissectible.WillInteract(interaction, side))
				{
					return false;
				}
			}

			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return CanBeConsumedBy(interaction.TargetObject);
		}

		/// <summary>
		/// Check thats eater can consume this item
		/// </summary>
		/// <param name="eater">Player that want to eat item</param>
		/// <returns></returns>
		public virtual bool CanBeConsumedBy(GameObject eater)
		{
			//todo: support npc force feeding
			var targetPlayer = eater.GetCachedComponent<PlayerScript>();
			if (targetPlayer == null || targetPlayer.IsDeadOrGhost || targetPlayer.IsNormal == false)
			{
				return false;
			}

			return true;
		}


		/// <summary>
		/// Try to consume this item by eater. Server side only.
		/// </summary>
		/// <param name="eater">Player that want to eat item</param>
		public void TryConsume(GameObject eater)
		{
			TryConsume(eater, eater);
		}

		/// <summary>
		/// Try to consume this item by eater. Server side only.
		/// </summary>
		/// <param name="feeder">Player that feed eater. Can be same as eater.</param>
		/// <param name="eater">Player that is going to eat item</param>
		public abstract void TryConsume(GameObject feeder, GameObject eater, bool projectileFed = false);
	}
}
