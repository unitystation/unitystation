using System;
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
///     Indicates an edible object.
/// </summary>
[RequireComponent(typeof(ItemAttributesV2))]
public class Edible : NetworkBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<HandApply>
{
	public GameObject leavings;
	protected virtual bool isDrink => false;
	protected string EatVerb => isDrink ? "drink" : "eat";

	private static readonly StandardProgressActionConfig ProgressConfig
		= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

	[FormerlySerializedAs("NutrientsHealAmount")]
	public int NutritionLevel = 10;

	protected ItemAttributesV2 itemAttributes;
	private Stackable stackable;

	private string Name => itemAttributes.ArticleName;

	private void Awake()
	{
		itemAttributes = GetComponent<ItemAttributesV2>();
		stackable = GetComponent<Stackable>();
		if (itemAttributes)
		{
			itemAttributes.AddTrait(CommonTraits.Instance.Food);
		}
		else
		{
			Logger.LogErrorFormat("{0} prefab is missing ItemAttributes", Category.ItemSpawn, name);
		}
	}

	/// <summary>
	/// Used by NPC's' server side
	/// </summary>
	public void NPCTryEat()
	{
		SoundManager.PlayNetworkedAtPos("EatFood", transform.position);
		if (leavings != null)
		{
			Spawn.ServerPrefab(leavings, transform.position, transform.parent);
		}

		Despawn.ServerSingle(gameObject);
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		return true;
	}
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//this item shouldn't be a target
		if (Validations.IsTarget(gameObject, interaction)) return false;

		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//todo: support npc force feeding
		var targetPlayer = interaction.TargetObject.GetComponent<PlayerScript>();
		if (targetPlayer == null || targetPlayer.IsDeadOrGhost)
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// Eat by activating from inventory
	/// </summary>
	public void ServerPerformInteraction(HandActivate interaction)
	{
		TryToEat(interaction.PerformerPlayerScript, interaction.PerformerPlayerScript);
	}

	/// <summary>
	/// Eat by clicking yourself or force feed someone else
	/// </summary>
	public void ServerPerformInteraction(HandApply interaction)
	{
		var targetPlayer = interaction.TargetObject.GetComponent<PlayerScript>();
		if (targetPlayer == null)
		{
			return;
		}

		PlayerScript feeder = interaction.PerformerPlayerScript;
		PlayerScript eater = targetPlayer;

		TryToEat(feeder, eater);
	}

	private void TryToEat(PlayerScript feeder, PlayerScript eater)
	{
		var feederSlot = feeder.ItemStorage.GetActiveHandSlot();
		if (feederSlot.Item == null)
		{	//Already been eaten or the food is no longer in hand
			return;
		}

		var eaterHungerState = eater.playerHealth.Metabolism.HungerState;

		if (feeder == eater) //If you're eating it yourself.
		{
			switch (eaterHungerState)
			{
				case HungerState.Full:
					Chat.AddActionMsgToChat(eater.gameObject, $"You cannot force any more of the {Name} to go down your throat!",
					$"{eater.playerName} cannot force any more of the {Name} to go down {eater.characterSettings.PossessivePronoun()} throat!");
					return; //Not eating!
				case HungerState.Normal:
					Chat.AddActionMsgToChat(eater.gameObject, $"You unwillingly {EatVerb} the {Name}.", //"a bit of"
						$"{eater.playerName} unwillingly {EatVerb}s the {Name}."); //"a bit of"
					break;
				case HungerState.Hungry:
					Chat.AddActionMsgToChat(eater.gameObject, $"You {EatVerb} the {Name}.",
						$"{eater.playerName} {EatVerb}s the {Name}.");
					break;
				case HungerState.Malnourished:
					Chat.AddActionMsgToChat(eater.gameObject, $"You hungrily {EatVerb} the {Name}.",
						$"{eater.playerName} hungrily {EatVerb}s the {Name}.");
					break;
				case HungerState.Starving:
					Chat.AddActionMsgToChat(eater.gameObject, $"You hungrily {EatVerb} the {Name}, gobbling it down!",
						$"{eater.playerName} hungrily {EatVerb}s the {Name}, gobbling it down!");
					break;
			}
		}
		else //If you're feeding it to someone else.
		{
			if (eaterHungerState == HungerState.Full)
			{
				Chat.AddActionMsgToChat(eater.gameObject,
					$"{feeder.playerName} cannot force any more of {Name} down your throat!",
					$"{feeder.playerName} cannot force any more of {Name} down {eater.playerName}'s throat!");
				return; //Not eating!
			}
			else
			{
				Chat.AddActionMsgToChat(eater.gameObject,
					$"{feeder.playerName} attempts to feed you {Name}.",
					$"{feeder.playerName} attempts to feed {eater.playerName} {Name}.");
			}

			//Wait 3 seconds before you can feed
			StandardProgressAction.Create(ProgressConfig, () =>
			{
				Chat.AddActionMsgToChat(eater.gameObject,
				$"{feeder.playerName} forces you to eat {Name}!",
				$"{feeder.playerName} forces {eater.playerName} to eat {Name}!");
				Eat();
			}).ServerStartProgress(eater.registerTile, 3f, feeder.gameObject);
			return;
		}

		Eat();

		void Eat()
		{
			SoundManager.PlayNetworkedAtPos(isDrink ? "Slurp" : "EatFood", eater.WorldPos);

			eater.playerHealth.Metabolism
				.AddEffect(new MetabolismEffect(NutritionLevel, 0, MetabolismDuration.Food));

			//If food has a stack component, decrease amount by one instead of deleting the entire stack.
			if (stackable != null)
			{
				stackable.ServerConsume(1);
			}
			else
			{
				Inventory.ServerDespawn(feederSlot);
			}

			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = Inventory.ServerAdd(pickupable, feederSlot);
				if (!added)
				{
					//If stackable has leavings and they couldn't go in the same slot, they should be dropped
					pickupable.CustomNetTransform.SetPosition(feeder.WorldPos);
				}
			}
		}
	}
}