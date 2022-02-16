using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using HealthV2;
using UnityEngine;
using NaughtyAttributes;


/// <summary>
/// Item that can be drinked or eaten by player
/// Also supports force feeding other player
/// </summary>
public abstract class Consumable : MonoBehaviour, ICheckedInteractable<HandApply>, IInteractable<HandActivate>, IRightClickable
{
	public bool requiresOpeningBeforeConsumption = false;
	protected bool isOpenForConsumption = false;

	[SerializeField, ShowIf(nameof(requiresOpeningBeforeConsumption))]
	private AddressableAudioSource openingNoise;
	[SerializeField, ShowIf(nameof(requiresOpeningBeforeConsumption))]
	private SpriteDataSO openedSprite;
	[SerializeField, ShowIf(nameof(requiresOpeningBeforeConsumption))]
	private SpriteHandler spriteHandler;
	[SerializeField, ShowIf(nameof(requiresOpeningBeforeConsumption))]
	private string openingVerb = "open";

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (requiresOpeningBeforeConsumption)
		{
			if (isOpenForConsumption == false)
			{
				Chat.AddExamineMsg(interaction.Performer, $"You need to open {gameObject.ExpensiveName()} before consuming it!");
				return;
			}
		}
		var targetPlayer = interaction.TargetObject.GetComponent<PlayerScript>();
		if (targetPlayer == null)
		{
			return;
		}

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

		if (!DefaultWillInteract.Default(interaction, side)) return false;

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
		var targetPlayer = eater.GetComponent<PlayerScript>();
		if (targetPlayer == null || targetPlayer.IsDeadOrGhost)
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
	public abstract void TryConsume(GameObject feeder, GameObject eater);

	/// <summary>
	/// For opening a consumable. Can be overriden by other scripts to extend this functionality even further.
	/// </summary>
	protected virtual void TryOpen()
	{
		if(requiresOpeningBeforeConsumption == false || isOpenForConsumption) return;
		isOpenForConsumption = true;
		if (openingNoise != null) SoundManager.PlayNetworkedAtPos(openingNoise, gameObject.AssumedWorldPosServer());
		if (openedSprite != null) spriteHandler.SetSpriteSO(openedSprite);
	}

	/// <summary>
	/// For opening the consumable and to trigger text feedback to the person holding it.
	/// </summary>
	/// <param name="activate"></param>
	protected virtual void TryOpen(HandActivate activate)
	{
		if(requiresOpeningBeforeConsumption == false || isOpenForConsumption) return;
		isOpenForConsumption = true;
		if (openingNoise != null) SoundManager.PlayNetworkedAtPos(openingNoise, gameObject.AssumedWorldPosServer());
		if (openedSprite != null) spriteHandler.SetSpriteSO(openedSprite);
		if (activate != null) Chat.AddExamineMsg(activate.Performer, $"You {openingVerb} the {gameObject.ExpensiveName()}");
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		TryOpen(interaction);
	}


	/// <summary>
	/// Generates a right click button for items like Space Cola where another script gets in the way of HandActivate
	/// </summary>
	/// <returns></returns>
	public RightClickableResult GenerateRightClickOptions()
	{
		var rightClickResult = new RightClickableResult();
		if (requiresOpeningBeforeConsumption == false) return rightClickResult;
		rightClickResult.AddElement("Open This", TryOpen);
		return rightClickResult;
	}
}
