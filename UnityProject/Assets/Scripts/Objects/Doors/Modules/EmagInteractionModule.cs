using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Doors.Modules
{
	public class EmagInteractionModule : DoorModuleBase
	{
		private BoltsModule BoltsModule;

		protected override void Awake()
		{
			base.Awake();
			BoltsModule = GetComponent<BoltsModule>();
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			var ItemStorage = interaction.Performer.GetComponent<DynamicItemStorage>();
			return EmagChecks(ItemStorage, interaction, States);
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			var ItemStorage = byPlayer.GetComponent<DynamicItemStorage>();
			return EmagChecks(ItemStorage, null, States);
		}

		/// <summary>
		/// Checks to see if a door can be emagged, does checks for BumpInteraction and Hand Interactions.
		/// </summary>
		/// <param name="itemStorage">The player's inventory that may contain the emag</param>
		/// <param name="interaction">If we're calling this from ClosedInteraction() to provide a HandApply</param>
		/// <param name="States">Door process states</param>
		/// <returns>Either hacked or ModuleSignal.Continue</returns>
		private ModuleSignal EmagChecks(DynamicItemStorage itemStorage, HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (itemStorage != null)
			{
				try
				{
					Emag emagInHand = itemStorage.GetActiveHandSlot().Item?.OrNull().gameObject.GetComponent<Emag>()?.OrNull();
					if (emagInHand != null)
					{
						if (interaction != null)
						{
							if (emagInHand.UseCharge(interaction)) return EmagSuccessLogic(States);
						}
						if (emagInHand.UseCharge(gameObject, itemStorage.registerPlayer.PlayerScript.gameObject)) return EmagSuccessLogic(States);
					}

					foreach (var item in itemStorage.GetNamedItemSlots(NamedSlot.id))
					{
						Emag emagInIdSlot = item.Item?.OrNull().gameObject.GetComponent<Emag>()?.OrNull();
						if (emagInIdSlot == null) continue;
						if (interaction != null)
						{
							if (emagInIdSlot.UseCharge(interaction)) return EmagSuccessLogic(States);
						}
						if (emagInIdSlot.UseCharge(gameObject, itemStorage.registerPlayer.PlayerScript.gameObject)) return EmagSuccessLogic(States);
					}
				}
				catch (NullReferenceException exception)
				{
					Logger.LogError(
						"A NRE was caught in EmagInteractionModule.ClosedInteraction() " + exception.Message,
						Category.Interaction);
				}
			}

			return ModuleSignal.Continue;
		}

		/// <summary>
		/// What happens after a door gets emagged.
		/// </summary>
		/// <returns>ModuleSignal.Continue</returns>
		private ModuleSignal EmagSuccessLogic(HashSet<DoorProcessingStates> States)
		{
			States.Add(DoorProcessingStates.SoftwareHacked);
			StartCoroutine(ToggleBolts());
			return ModuleSignal.Continue;
		}

		private IEnumerator ToggleBolts()
		{
			yield return null;
			BoltsModule.OrNull()?.PulseToggleBolts(true);
		}

	}
}
