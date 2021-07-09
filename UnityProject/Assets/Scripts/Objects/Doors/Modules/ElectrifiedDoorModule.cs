using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Electricity;
using HealthV2;

namespace Doors.Modules
{
	public class ElectrifiedDoorModule : DoorModuleBase
	{
		[SerializeField] private int voltageDamage = 9080;
		[SerializeField] private bool isElectrecuted = false;

		public bool IsElectrecuted
		{
			get => isElectrecuted;
			set => isElectrecuted = value;
		}

		public override ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return CanElectricute(interaction?.Performer);
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return CanElectricute(interaction.Performer);
		}

		public override ModuleSignal BumpingInteraction(GameObject mob, HashSet<DoorProcessingStates> States)
		{
			return CanElectricute(mob);
		}

		private ModuleSignal CanElectricute(GameObject mob)
		{
			if (master.HasPower && IsElectrecuted)
			{
				if (PlayerHasInsulatedGloves(mob) == false)
				{
					ServerElectrocute(mob);
					return ModuleSignal.Break;
				}
				return ModuleSignal.ContinueRegardlessOfOtherModulesStates;
			}

			return ModuleSignal.Continue;
		}

		private bool PlayerHasInsulatedGloves(GameObject mob)
		{
			List<ItemSlot> slots = mob.GetComponent<PlayerScript>().OrNull()?.DynamicItemStorage.OrNull()?.GetNamedItemSlots(NamedSlot.hands);
			if (slots != null)
			{
				foreach (ItemSlot slot in slots)
				{
					if (Validations.HasItemTrait(slot.ItemObject, CommonTraits.Instance.Insulated))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void ServerElectrocute(GameObject obj)
		{
			LivingHealthMasterBase healthScript = obj.GetComponent<LivingHealthMasterBase>();
			if (healthScript != null)
			{
				var electrocution = new Electrocution(voltageDamage, master.RegisterTile.WorldPositionServer, "wire"); //More magic numbers.
				healthScript.Electrocute(electrocution);
			}
		}

		public override bool CanDoorStateChange()
		{
			if (master.HasPower && IsElectrecuted)
			{
				return false;
			}
			return true;
		}
	}
}