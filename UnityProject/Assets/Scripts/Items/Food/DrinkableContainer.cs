using Chemistry.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items;
using ScriptableObjects;
using UnityEngine;
using AddressableReferences;
using Messages.Server.SoundMessages;
using HealthV2;
using Random = UnityEngine.Random;
using WebSocketSharp;

[RequireComponent(typeof(ItemAttributesV2))]
[RequireComponent(typeof(ReagentContainer))]
public class DrinkableContainer : Consumable
{
	/// <summary>
	/// The name of the sound the player makes when drinking
	/// </summary>
	[Tooltip("The name of the sound the player makes when drinking (must be in soundmanager")]
	[SerializeField] private AddressableAudioSource drinkSound = null;

	private float RandomPitch => Random.Range( 0.7f, 1.3f );

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
			Chat.AddExamineMsgFromServer(eater.gameObject, $"The {gameObject.ExpensiveName()} is empty.");
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
		var drinkAmount = container.TransferAmount;
		int stomachIndex = 0;
		List<Stomach> stomachs = eater.playerHealth.GetStomachs();
		while (drinkAmount > 0)
		{
			ReagentContainer stomachContainer = stomachs[stomachIndex].StomachContents;
			if (stomachContainer.IsFull)
			{
				if (stomachIndex < stomachs.Count-1)
				{
					stomachIndex++;
					continue;
				}
				break;
			}

			float transferred = Mathf.Min(drinkAmount,
				stomachContainer.MaxCapacity - stomachContainer.CurrentReagentMix.Total);
			container.TransferTo(transferred, stomachContainer);
			drinkAmount -= transferred;
		}

		// Play sound
		if (item && drinkSound != null)
		{
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(RandomPitch, spatialBlend: 1f);
			SoundManager.PlayNetworkedAtPos(drinkSound, eater.WorldPos, audioSourceParameters, sourceObj: eater.gameObject);
		}
	}

	private void DoDrinkEffects(PlayerScript eater, float drinkAmount)
	{
		var playerEatDrinkEffects = eater.GetComponent<PlayerEatDrinkEffects>();

		if(playerEatDrinkEffects == null) return;

		if ((int) drinkAmount == 0) return;

		foreach (var reagent in container.CurrentReagentMix.reagents.m_dict)
		{
			//if its not alcoholic skip
			if (!AlcoholicDrinksSOScript.Instance.AlcoholicReagents.Contains(reagent.Key)) continue;

			//The more different types of alcohol in a drink the longer you get drunk for each sip.
			playerEatDrinkEffects.ServerSendMessageToClient(eater.gameObject, (int)drinkAmount);
		}
	}
}
