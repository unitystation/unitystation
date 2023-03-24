using System.Collections.Generic;
using UnityEngine;
using HealthV2;
using Messages.Server;
using Mirror;
using UnityEngine.Serialization;

namespace Doors.Modules
{
	public class ElectrifiedDoorModule : DoorModuleBase, IServerLifecycle
	{
		[SerializeField]
		private int voltageDamage = 9080;

		[SerializeField]
		private bool isElectrified = false;

		[SerializeField]
		private SpriteHandler spriteHandler = null;

		public static HashSet<ElectrifiedDoorModule> ElectrifiedDoors { get; private set; } = new HashSet<ElectrifiedDoorModule>();

		public bool IsElectrified
		{
			get => isElectrified;
			set
			{
				isElectrified = value;

				if (isElectrified)
				{
					AddToElectrified(this);
				}
				else
				{
					RemoveFromElectrified(this);
				}
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			master.HackingProcessBase.RegisterPort(ToggleElectrocution, master.GetType());
			master.HackingProcessBase.RegisterPort(PreventElectrocution, master.GetType());

			if(isElectrified == false) return;
			AddToElectrified(this);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			RemoveFromElectrified(this);
		}

		public void ToggleElectrocutionInput()
		{
			master.HackingProcessBase.ImpulsePort(ToggleElectrocution);
		}

		public void ToggleElectrocution()
		{
			if (master.HasPower == false)
				return;
			IsElectrified = !IsElectrified;
		}

		public override void OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null)
			{
				return;
			}

			CanElectricute(interaction.Performer);
		}

		public override void ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null)
			{
				return;
			}

			CanElectricute(interaction.Performer);
		}

		public override void BumpingInteraction(GameObject mob, HashSet<DoorProcessingStates> States)
		{
			CanElectricute(mob);
		}

		public bool PulsePreventElectrocution()
		{
			return master.HackingProcessBase.PulsePortConnectedNoLoop(PreventElectrocution, true);
		}

		public void PreventElectrocution()
		{
			master.HackingProcessBase.ReceivedPulse(PreventElectrocution);
		}

		private void CanElectricute(GameObject mob)
		{
			if (master.HasPower)
			{
				if (IsElectrified == false)
				{
					if (PulsePreventElectrocution())
					{
						if (PlayerHasInsulatedGloves(mob) == false)
						{
							ServerElectrocute(mob);
							return;
						}

						return;
					}
				}
				else
				{
					if (PlayerHasInsulatedGloves(mob) == false)
					{
						ServerElectrocute(mob);
						return;
					}

					return;
				}
			}
			return;
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


		#region Synthetic sprite

		private static void AddToElectrified(ElectrifiedDoorModule electrifiedDoor)
		{
			ElectrifiedDoors.Add(electrifiedDoor);

			ElectrifiedDoorMessage.Send(electrifiedDoor.master, true);
		}

		private static void RemoveFromElectrified(ElectrifiedDoorModule electrifiedDoor)
		{
			ElectrifiedDoors.Remove(electrifiedDoor);

			ElectrifiedDoorMessage.Send(electrifiedDoor.master, false);
		}

		public void NewSpriteState(bool state)
		{
			spriteHandler.ChangeSprite(state ? 1 : 0, false);
		}

		public static void Rejoined(NetworkConnectionToClient conn)
		{
			foreach (var electrifiedDoor in ElectrifiedDoors)
			{
				ElectrifiedDoorMessage.SendTo(conn, electrifiedDoor.master, electrifiedDoor.isElectrified);
			}
		}

		public static void LeftBody(NetworkConnectionToClient conn)
		{
			foreach (var electrifiedDoor in ElectrifiedDoors)
			{
				ElectrifiedDoorMessage.SendTo(conn, electrifiedDoor.master, false);
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStatics()
		{
			ElectrifiedDoors = new HashSet<ElectrifiedDoorModule>();
		}

		#endregion
	}
}
