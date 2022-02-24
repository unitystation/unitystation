using System.Collections.Generic;
using UnityEngine;
using HealthV2;

namespace Doors.Modules
{
	public class ElectrifiedDoorModule : DoorModuleBase, IServerSpawn
	{
		[SerializeField] private int voltageDamage = 9080;
		[SerializeField] private bool isElectrecuted = false;

		public bool IsElectrecuted
		{
			get => isElectrecuted;
			set => isElectrecuted = value;
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			master.HackingProcessBase.RegisterPort(ToggleElectrocution, master.GetType());
			master.HackingProcessBase.RegisterPort(PreventElectrocution, master.GetType());
		}

		public void ToggleElectrocutionInput()
		{
			master.HackingProcessBase.ImpulsePort(ToggleElectrocution);
		}

		public void ToggleElectrocution()
		{
			if (master.HasPower == false)
				return;
			IsElectrecuted = !IsElectrecuted;
		}

		public override ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null)
			{
				return ModuleSignal.Continue;
			}

			return CanElectricute(interaction.Performer);
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return CanElectricute(interaction.Performer);
		}

		public override ModuleSignal BumpingInteraction(GameObject mob, HashSet<DoorProcessingStates> States)
		{
			return CanElectricute(mob);
		}

		public bool PulsePreventElectrocution()
		{
			return master.HackingProcessBase.PulsePortConnectedNoLoop(PreventElectrocution, true);
		}

		public void PreventElectrocution()
		{
			master.HackingProcessBase.ReceivedPulse(PreventElectrocution);
		}

		private ModuleSignal CanElectricute(GameObject mob)
		{
			if (master.HasPower)
			{
				if (IsElectrecuted == false)
				{
					if (PulsePreventElectrocution())
					{
						if (PlayerHasInsulatedGloves(mob) == false)
						{
							ServerElectrocute(mob);
							return ModuleSignal.Continue;
						}

						return ModuleSignal.ContinueRegardlessOfOtherModulesStates;
					}
				}
				else
				{
					if (PlayerHasInsulatedGloves(mob) == false)
					{
						ServerElectrocute(mob);
						return ModuleSignal.Continue;
					}

					return ModuleSignal.ContinueRegardlessOfOtherModulesStates;
				}
			}
			return ModuleSignal.Continue;
		}

		private bool PlayerHasInsulatedGloves(GameObject mob)
		{
			List<ItemSlot> slots = mob.GetComponent<PlayerScript>().OrNull()?.DynamicItemStorage.OrNull()
				?.GetNamedItemSlots(NamedSlot.hands);
			if (slots == null) return false;
			foreach (ItemSlot slot in slots)
			{
				if(slot.IsEmpty) continue;
				if (Validations.HasItemTrait(slot.ItemObject, CommonTraits.Instance.Insulated))
				{
					Chat.AddExamineMsg(mob, "You feel a tingle go through your hand.");
					return true;
				}
			}

			return false;
		}

		private void ServerElectrocute(GameObject obj)
		{
			LivingHealthMasterBase healthScript = obj.GetComponent<LivingHealthMasterBase>();
			if (healthScript == null) return;
			var electrocution =
				new Electrocution(voltageDamage, master.RegisterTile.WorldPositionServer,
					"wire"); //More magic numbers.
			healthScript.Electrocute(electrocution);
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
