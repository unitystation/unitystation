using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.HealthV2.Living.Metabolism;
using US13.Items.Implants.Organs;
using US13.Managers;
using US13.Player;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Core.ProgressBar;
using Util;

namespace US13.Items.Food
{
	/// <summary>
	/// Drinkable container (cups, bottles, cans) that transfers reagents to the eater's stomachs.
	/// </summary>
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(ReagentContainer))]
	public class DrinkableContainer : Consumable
	{
		[Tooltip("The name of the sound the player makes when drinking (must be in soundmanager")]
		[SerializeField] private AddressableAudioSource drinkSound = null;

		private ReagentContainer container;
		private ItemAttributesV2 itemAttributes;
		private RegisterItem item;

		/// <summary>If true, always transfers exactly <see cref="FixedORMAXTransferAmount"/> units.</summary>
		public bool SetFixedTransfer = false;

		/// <summary>If true, caps each transfer to <see cref="FixedORMAXTransferAmount"/> units.</summary>
		public bool MaxTransfer = true;

		private bool ShowTransfer => SetFixedTransfer || MaxTransfer;

		[ShowIf(nameof(ShowTransfer))]
		public int FixedORMAXTransferAmount = 5;


		private static readonly StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

		private void Awake()
		{
			container = this.GetCachedComponent<ReagentContainer>(includeDisabled: false);
			itemAttributes = GetComponent<ItemAttributesV2>();
			item = GetComponent<RegisterItem>();
		}

		public override void TryConsume(GameObject feederGO, GameObject eaterGO, bool projectileFed = false)
		{
			if (!container)
				return;

			// todo: make seperate logic for NPC
			var eater = eaterGO.GetComponent<PlayerScript>();
			var feeder = feederGO.GetComponent<PlayerScript>();
			if (eater == null || feeder == null)
				return;

			// Check if player is wearing clothing that prevents eating or drinking
			if (eater.Equipment.CanConsume() == false)
			{
				Chat.AddExamineMsgFromServer(eater.gameObject, $"Remove items that cover your mouth first!");
				return;
			}
			// Check if container is empty
			var reagentUnits = container.Total;
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
				}).ServerStartProgress(eater.RegisterPlayer, 3f, feeder.gameObject);
				return;
			}
			else
			{
				Drink(eater, feeder);
			}
		}

		/// <summary>
		/// Transfers reagents from this container into the eater's stomachs, respecting transfer amount
		/// settings. Fires <see cref="Consumable.OnConsumed"/> on completion.
		/// </summary>
		public virtual void Drink(PlayerScript eater, PlayerScript feeder)
		{
			var drinkAmount = container.TransferAmount;
			// Start drinking reagent mix
			if (SetFixedTransfer)
			{
				drinkAmount = FixedORMAXTransferAmount;
			}

			if (MaxTransfer)
			{
				drinkAmount = Mathf.Min(drinkAmount, FixedORMAXTransferAmount);
			}



			List<Stomach> stomachs = eater.playerHealth.GetStomachs();
			foreach (Stomach currentStomach in stomachs)
			{
				ReagentContainer stomachContainer = currentStomach.StomachContents;

				//fill current stomach as much as we can until empty
				float transferred = Mathf.Min(drinkAmount,
					stomachContainer.MaxCapacity - stomachContainer.CurrentReagentMix.Total);
				container.TransferTo(transferred, stomachContainer);

				//update how much is left
				drinkAmount -= transferred;

				//Yeetity, it's empty
				if (drinkAmount <= 0) break;

				//We didn't empty the drink, but maybe emptying the drink was the friends we made along the way
				if (stomachs.LastOrDefault() == currentStomach)
				{
					Chat.AddExamineMsgFromServer(eater.gameObject,"You cannot drink anymore!");
					if(eater != feeder)
						Chat.AddExamineMsgFromServer(feeder.gameObject,$"{eater.visibleName} cannot seem to drink anymore!");
				}
			}

			// Play sound
			if (item && drinkSound != null)
			{
				Sound.At(drinkSound, eater.gameObject)
					.WithRandomPitch(0.7f, 1.3f)
					.PlayNetworked();
			}

			InvokeOnConsumed(eater.gameObject, feeder.gameObject);
		}
	}
}
