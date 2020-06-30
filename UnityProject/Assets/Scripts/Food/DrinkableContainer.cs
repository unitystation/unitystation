using Chemistry.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ItemAttributesV2))]
[RequireComponent(typeof(ReagentContainer))]
public class DrinkableContainer : Consumable
{
	public string sound = "Slurp";

	private ReagentContainer container;
	private ItemAttributesV2 itemAttributes;
	private RegisterItem item;


	private static readonly StandardProgressActionConfig ProgressConfig
		= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

	private void Awake()
	{
		container = GetComponent<ReagentContainer>();
		itemAttributes = GetComponent<ItemAttributesV2>();
		item = GetComponent<RegisterItem>();
	}

	public override void TryConsume(GameObject feederGO, GameObject eaterGO)
	{
		if (!container)
			return;

		// todo: make seperate logic for NPC
		var eater = eaterGO.GetComponent<PlayerScript>();
		var feeder = feederGO.GetComponent<PlayerScript>();
		if (eater == null || feeder == null)
			return;

		// Check if container is empty
		var reagentUnits = container.ReagentMixTotal;
		if (reagentUnits <= 0f)
		{
			Chat.AddExamineMsgFromServer(eater.gameObject, $"{gameObject.ExpensiveName()} is empty.");
			return;
		}

		// Get current container name
		var name = itemAttributes ? itemAttributes.ArticleName : gameObject.ExpensiveName();
		// Generate message to player
		ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, HungerState.Hungry, name, "drink");

		if (feeder != eater)  //If you're feeding it to someone else.
		{
			//Wait 3 seconds before you can feed
			StandardProgressAction.Create(ProgressConfig, () =>
			{
				ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, HungerState.Hungry, name, "drink");
				Drink(eater, feeder);
			}).ServerStartProgress(eater.registerTile, 3f, feeder.gameObject);
			return;
		}
		else
		{
			Drink(eater, feeder);
		}
	}

	private void Drink(PlayerScript eater, PlayerScript feeder)
	{
		// Start drinking reagent mix
		// todo: actually transfer reagent mix inside player stomach
		var drinkAmount = container.TransferAmount;
		container.TakeReagents(drinkAmount);

		// Play sound
		if (item && !string.IsNullOrEmpty(sound))
		{
			SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);
		}

	}
}
