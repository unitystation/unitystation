using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Systems.Explosions;
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

		public override void ClosedInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return;
			var ItemStorage = interaction.Performer.GetComponent<DynamicItemStorage>();
			var Item = ItemStorage.OrNull()?.GetActiveHandSlot()?.Item;

			if (Item != null)
			{
				var hasEmag = Item.OrNull()?.gameObject.OrNull()
					?.GetComponent<Emag>()?.OrNull();
				if (hasEmag == null) return;
			}
			EmagChecks(ItemStorage, interaction, ref States);
		}

		public override void BumpingInteraction(GameObject byPlayer, ref HashSet<DoorProcessingStates> States)
		{
			if (byPlayer == null) return; //null may appear if door wires are pulsed by EMP
			var ItemStorage = byPlayer.GetComponent<DynamicItemStorage>();
			EmagChecks(ItemStorage, null, ref States);
		}

		/// <summary>
		/// Checks to see if a door can be emagged, does checks for BumpInteraction and Hand Interactions.
		/// </summary>
		/// <param name="itemStorage">The player's inventory that may contain the emag</param>
		/// <param name="interaction">If we're calling this from ClosedInteraction() to provide a HandApply</param>
		/// <param name="States">Door process states</param>
		/// <returns>Either hacked or ModuleSignal.Continue</returns>
		private void EmagChecks(DynamicItemStorage itemStorage, HandApply interaction,
			ref HashSet<DoorProcessingStates> States)
		{
			if (itemStorage != null)
			{
				Emag emagInHand = itemStorage.OrNull()?.GetActiveHandSlot()?.Item.OrNull()?.gameObject.OrNull()?.GetComponent<Emag>()?.OrNull();
				if (emagInHand != null)
				{
					if (interaction != null)
					{
						if (emagInHand.UseCharge(interaction))
						{
							EmagSuccessLogic(ref States);
							return;
						}
					}

					if (emagInHand.UseCharge(gameObject, itemStorage.registerPlayer.PlayerScript.gameObject))
					{
						EmagSuccessLogic(ref States);
						return;
					}

				}

				foreach (var item in itemStorage.GetNamedItemSlots(NamedSlot.id))
				{
					Emag emagInIdSlot = item?.Item.OrNull()?.gameObject.GetComponent<Emag>()?.OrNull();
					if (emagInIdSlot == null) continue;
					if (interaction != null)
					{
						if (emagInIdSlot.UseCharge(interaction))
						{
							EmagSuccessLogic(ref States);
							return;
						}
					}

					if (emagInIdSlot.UseCharge(gameObject, itemStorage.registerPlayer.PlayerScript.gameObject))
					{
						EmagSuccessLogic(ref States);
						return;
					}
				}
			}
		}

		/// <summary>
		/// What happens after a door gets emagged.
		/// </summary>
		/// <returns>ModuleSignal.Continue</returns>
		private void EmagSuccessLogic(ref HashSet<DoorProcessingStates> States)
		{
			States.Add(DoorProcessingStates.SoftwareHacked);
			if (States.Contains(DoorProcessingStates.SoftwarePrevented))
			{
				States.Remove(DoorProcessingStates.SoftwarePrevented);
			}
			StartCoroutine(ToggleBolts());
			SparkUtil.TrySpark(master.gameObject);
		}

		private IEnumerator ToggleBolts()
		{
			yield return null;
			BoltsModule.OrNull()?.PulseToggleBolts(true);
		}
	}
}