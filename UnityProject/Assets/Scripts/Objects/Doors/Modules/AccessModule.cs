using Systems.Clearance;
using System.Collections.Generic;
using UnityEngine;
using Systems.Electricity;
using Random = UnityEngine.Random;

namespace Doors.Modules
{
	[RequireComponent(typeof(ClearanceRestricted))]
	public class AccessModule : DoorModuleBase, IServerSpawn
	{
		public ClearanceRestricted ClearanceRestricted { get; private set; }
		public PowerModule PowerModule { get; private set; }
		private bool emergencyAccess = false;

		[SerializeField]
		[Tooltip("When the door is at low voltage, this is the chance that the access check gives a false positive.")]
		private float lowVoltageOpenChance = 0.05f;

		protected override void Awake()
		{
			base.Awake();
			ClearanceRestricted = GetComponent<ClearanceRestricted>();
			PowerModule = GetComponent<PowerModule>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			master.HackingProcessBase.RegisterPort(GrantAccess, master.GetType());
		}


		public override void OpenInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			// We check access only if the door has power and it isn't emagged, in which case we still deny but don't play a sound
			// Emagging is in its own module and could play another sound we don't want to overlap. Interaction can be null for several 
			// reasons, we don't want to play the access denied sound for them either, the door only makes a sound for legit access denials
			if (interaction != null && master.HasPower && States.Contains(DoorProcessingStates.SoftwareHacked) == false)
				CheckAccess(interaction.Performer, States);
			else
				States.Add(DoorProcessingStates.SoftwarePrevented);
		}

		public override void ClosedInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			// We check access only if the door has power and it isn't emagged, in which case we still deny but don't play a sound
			// Emagging is in its own module and could play another sound we don't want to overlap. Interaction can be null for several 
			// reasons, we don't want to play the access denied sound for them either, the door only makes a sound for legit access denials
			if (interaction != null && master.HasPower && States.Contains(DoorProcessingStates.SoftwareHacked) == false)
				CheckAccess(interaction.Performer, States);
			else
				States.Add(DoorProcessingStates.SoftwarePrevented);
		}

		public override void BumpingInteraction(GameObject byPlayer, ref HashSet<DoorProcessingStates> States)
		{
			// We check access only if the door has power and it isn't emagged, in which case we still deny but don't play a sound
			// Emagging is in its own module and could play another sound we don't want to overlap. Interaction can be null for several 
			// reasons, we don't want to play the access denied sound for them either, the door only makes a sound for legit access denials
			if (byPlayer != null && master.HasPower && States.Contains(DoorProcessingStates.SoftwareHacked) == false)
				CheckAccess(byPlayer, States);
			else
				States.Add(DoorProcessingStates.SoftwarePrevented);
		}

		public void CheckAccess(GameObject player, HashSet<DoorProcessingStates> States)
		{
			// If the GrantAccess wire isn't cut (the system always denies in that case) and the player has clearance or
			// the emergency state allows anyone to use the door, we don't need to set a state.
			// We also want it so if the door is in low voltage, there's a very low chance the access check fails 
			// and opens anyway, to simulate the kind of weird flux state bits are when in low voltage systems.
			if (master.HackingProcessBase.HasConnection(this.GrantAccess) == false || (ClearanceRestricted.HasClearance(player) == false &&
				emergencyAccess == false && (PowerModule.Apc.State == PowerState.LowVoltage && Random.value < lowVoltageOpenChance) == false))
			{
				States.Add(DoorProcessingStates.SoftwarePrevented);

				master.DoorAnimator.PlayDeniedAnimation().Forget();
				master.SoundController.ServerPlaySound(DoorSoundController.DoorSoundType.AccessDenied);
			}

			// We pulse the port in case of hacking shennanigans
			master.HackingProcessBase.ImpulsePort(GrantAccess);
		}

		public void GrantAccess() { }

		public void ToggleAuthorizationBypassState()
		{
			//TODO : Add emergency access lights to airlocks
			emergencyAccess = !emergencyAccess;
		}
	}
}
