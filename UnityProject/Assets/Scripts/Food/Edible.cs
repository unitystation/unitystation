using System;
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
/// Indicates an edible object
/// </summary>
[RequireComponent(typeof(RegisterItem))]
[RequireComponent(typeof(ItemAttributesV2))]
public class Edible : Consumable, ICheckedInteractable<HandActivate>
{
	public GameObject leavings;

	public string sound = "EatFood";

	private static readonly StandardProgressActionConfig ProgressConfig
		= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

	[FormerlySerializedAs("NutrientsHealAmount")]
	public int NutritionLevel = 10;

	protected ItemAttributesV2 itemAttributes;
	private Stackable stackable;
	private RegisterItem item;

	private string Name => itemAttributes.ArticleName;

	private void Awake()
	{
		item = GetComponent<RegisterItem>();
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

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		return true;
	}

	/// <summary>
	/// Eat by activating from inventory
	/// </summary>
	public void ServerPerformInteraction(HandActivate interaction)
	{
		TryConsume(interaction.PerformerPlayerScript.gameObject);
	}

	public override void TryConsume(GameObject feederGO, GameObject eaterGO)
	{
		var eater = eaterGO.GetComponent<PlayerScript>();
		if (eater == null)
		{
			// todo: implement non-player eating
			SoundManager.PlayNetworkedAtPos(sound, item.WorldPosition);
			if (leavings != null)
			{
				Spawn.ServerPrefab(leavings, item.WorldPosition, transform.parent);
			}

			Despawn.ServerSingle(gameObject);
			return;
		}

		var feeder = feederGO.GetComponent<PlayerScript>();

		// Show eater message
		var eaterHungerState = eater.playerHealth.Metabolism.HungerState;
		ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, eaterHungerState, Name, "eat");

		// Check if eater can eat anything
		if (eaterHungerState != HungerState.Full)
		{
			if (feeder != eater)  //If you're feeding it to someone else.
			{
				//Wait 3 seconds before you can feed
				StandardProgressAction.Create(ProgressConfig, () =>
				{
					ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, eaterHungerState, Name, "eat");
					Eat(eater, feeder);
				}).ServerStartProgress(eater.registerTile, 3f, feeder.gameObject);
				return;
			}
			else
			{
				Eat(eater, feeder);
			}
		}
	}

	public virtual void Eat(PlayerScript eater, PlayerScript feeder)
	{
		SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

		eater.playerHealth.Metabolism
			.AddEffect(new MetabolismEffect(NutritionLevel, 0, MetabolismDuration.Food));

		var feederSlot = feeder.ItemStorage.GetActiveHandSlot();
		//If food has a stack component, decrease amount by one instead of deleting the entire stack.
		if (stackable != null)
		{
			stackable.ServerConsume(1);
		}
		else
		{
			Inventory.ServerDespawn(gameObject);
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
