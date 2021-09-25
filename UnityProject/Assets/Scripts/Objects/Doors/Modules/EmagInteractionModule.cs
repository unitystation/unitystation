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
			if (interaction != null)
			{
				try
				{
					var ItemStorage = interaction.Performer.GetComponent<DynamicItemStorage>();
					if (ItemStorage != null)
					{
						var Hand = ItemStorage.GetActiveHandSlot().ItemAttributes;
						if (Hand != null)
						{
							if (Hand.HasTrait(CommonTraits.Instance.Emag))
							{
								States.Add(DoorProcessingStates.SoftwareHacked);
								StartCoroutine(ToggleBolts());
								return ModuleSignal.Continue;
							}
						}

						foreach (var item in ItemStorage.GetNamedItemSlots(NamedSlot.id))
						{
							var ID = item.ItemAttributes;

							if (ID != null)
							{
								if (ID.HasTrait(CommonTraits.Instance.Emag))
								{
									States.Add(DoorProcessingStates.SoftwareHacked);
									StartCoroutine(ToggleBolts());
									return ModuleSignal.Continue;
								}
							}
						}
					}
				}
				catch (NullReferenceException exception)
				{
					Logger.LogError("A NRE was caught in EmagInteractionModule.ClosedInteraction() " + exception.Message, Category.Interaction);
				}
			}

			return ModuleSignal.Continue;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			var ItemStorage = byPlayer.GetComponent<DynamicItemStorage>();
			if (ItemStorage != null)
			{
				try
				{
					var Hand = ItemStorage.GetActiveHandSlot().ItemAttributes;
					if (Hand != null)
					{
						if (Hand.HasTrait(CommonTraits.Instance.Emag))
						{
							States.Add(DoorProcessingStates.SoftwareHacked);
							StartCoroutine(ToggleBolts());
							return ModuleSignal.Continue;
						}
					}
				}
				catch (NullReferenceException exception)
				{
					Logger.LogError(
						"A NRE was caught in EmagInteractionModule.BumpingInteraction(): " + exception.Message,
						Category.Interaction);
					return ModuleSignal.ContinueWithoutDoorStateChange;
				}

				foreach (var item in ItemStorage.GetNamedItemSlots(NamedSlot.id))
				{
					var ID = item.ItemAttributes;

					if (ID != null)
					{
						if (ID.HasTrait(CommonTraits.Instance.Emag))
						{
							States.Add(DoorProcessingStates.SoftwareHacked);
							StartCoroutine(ToggleBolts());
							return ModuleSignal.Continue;
						}
					}
				}
			}

			return ModuleSignal.Continue;
		}

		private IEnumerator ToggleBolts()
		{
			yield return null;
			BoltsModule.OrNull()?.PulseToggleBolts(true);
		}

	}
}