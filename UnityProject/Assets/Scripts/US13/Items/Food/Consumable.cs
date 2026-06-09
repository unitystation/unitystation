using System;
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
	/// Abstract base for any item that can be eaten or drunk.
	/// Subclasses must call <see cref="InvokeOnConsumed"/> after consumption for
	/// <see cref="EffectOnConsumed"/> and other listeners to fire.
	/// </summary>
	public abstract class Consumable : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField] protected float consumeTime = 0.1f;

		/// <summary>
		/// Raised server-side after consumption. Args: (eater, feeder).
		/// <see cref="EffectOnConsumed"/> subscribes to this to apply data-driven effects.
		/// </summary>
		public event Action<GameObject, GameObject> OnConsumed;

		/// <summary>
		/// Null-safe raise of <see cref="OnConsumed"/>. All subclasses must call this after a successful consume.
		/// </summary>
		protected void InvokeOnConsumed(GameObject eater, GameObject feeder)
		{
			OnConsumed?.Invoke(eater, feeder);
		}

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
			if (enabled == false) return false;
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
		/// Whether <paramref name="eater"/> is in a valid state to consume (alive, not ghost, normal state).
		/// </summary>
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


		public void TryConsume(GameObject eater)
		{
			TryConsume(eater, eater);
		}

		/// <summary>
		/// Server-side consume entry point. Must call <see cref="InvokeOnConsumed"/> on success.
		/// </summary>
		public abstract void TryConsume(GameObject feeder, GameObject eater, bool projectileFed = false);
	}
}
