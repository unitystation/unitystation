using Systems.Clearance;
 using System.Collections.Generic;
using UnityEngine;
using Systems.Electricity;
using Initialisation;
using Random = UnityEngine.Random;

namespace Doors.Modules
{
	[RequireComponent(typeof(AccessRestrictions))]
	public class AccessModule : DoorModuleBase
	{
		private AccessRestrictions accessRestrictions;
		private ClearanceCheckable clearanceCheckable;

		private GameObject Inplayer;

		[SerializeField]
		[Tooltip("When the door is at low voltage, this is the chance that the access check gives a false positive.")]
		private float lowVoltageOpenChance = 0.05f;

		protected override void Awake()
		{
			base.Awake();
			accessRestrictions = GetComponent<AccessRestrictions>();
			clearanceCheckable = GetComponent<ClearanceCheckable>();
			LoadManager.RegisterActionDelayed(DelayedRegister, 2);
		}


		public void DelayedRegister()
		{
			master.HackingProcessBase.RegisterPort(ProcessCheckAccess, master.GetType());
		}


		public override ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction != null)
			{
				if (!master.HasPower || !CheckAccess(interaction.Performer))
				{
					States.Add(DoorProcessingStates.SoftwarePrevented);
				}
			}

			return ModuleSignal.Continue;
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction != null)
			{
				if (!master.HasPower || !CheckAccess(interaction.Performer))
				{
					States.Add(DoorProcessingStates.SoftwarePrevented);
				}
			}

			return ModuleSignal.Continue;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			if (!master.HasPower || !CheckAccess(byPlayer))
			{
				States.Add(DoorProcessingStates.SoftwarePrevented);
			}

			return ModuleSignal.Continue;
		}


		private bool CheckAccess(GameObject player)
		{
			Inplayer = player;
			master.HackingProcessBase.ImpulsePort(ProcessCheckAccess);

			if (Inplayer == null)
			{
				Inplayer = null;
				return false;
			}
			else
			{
				Inplayer = null;
				return true;
			}
		}


		private void ProcessCheckAccess()
		{
			if (accessRestrictions.CheckAccess(Inplayer))
			{
				return;
			}

			//If the door is in low voltage, there's a very low chance the access check fails and opens anyway.
			//Meant to represent the kind of weird flux state bits are when in low voltage systems.
			if (master.Apc.State == PowerState.LowVoltage)
			{
				if (Random.value < lowVoltageOpenChance)
				{
					return;
				}
			}

			DenyAccess();
			Inplayer = null;
		}

		private void DenyAccess()
		{
			StartCoroutine(master.DoorAnimator.PlayDeniedAnimation());
		}
	}
}
