using UnityEngine;
using Systems.Electricity;
using Random = UnityEngine.Random;

namespace Doors.Modules
{
	[RequireComponent(typeof(AccessRestrictions))]
	public class AccessModule : DoorModuleBase
	{
		private AccessRestrictions accessRestrictions;

		[SerializeField]
		[Tooltip("When the door is at low voltage, this is the chance that the access check gives a false positive.")]
		private float lowVoltageOpenChance = 0.05f;

		protected override void Awake()
		{
			base.Awake();
			accessRestrictions = GetComponent<AccessRestrictions>();
		}

		public override ModuleSignal OpenInteraction(HandApply interaction)
		{
			return ModuleSignal.Continue;
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction)
		{
			if (!master.HasPower || !CheckAccess(interaction.Performer))
			{
				return ModuleSignal.ContinueWithoutDoorStateChange;
			}

			return ModuleSignal.Continue;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer)
		{
			if (!master.HasPower || !CheckAccess(byPlayer))
			{
				return ModuleSignal.ContinueWithoutDoorStateChange;
			}

			return ModuleSignal.Continue;
		}

		public override bool CanDoorStateChange()
		{
			return true;
		}

		private bool CheckAccess(GameObject player)
		{
			if (accessRestrictions.CheckAccess(player))
			{
				return true;
			}

			//If the door is in low voltage, there's a very low chance the access check fails and opens anyway.
			//Meant to represent the kind of weird flux state bits are when in low voltage systems.
			if (master.Apc.State == PowerState.LowVoltage)
			{
				if (Random.value < lowVoltageOpenChance)
				{
					return true;
				}
			}

			DenyAccess();
			return false;
		}

		private void DenyAccess()
		{
			StartCoroutine(master.DoorAnimator.PlayDeniedAnimation());
		}
	}
}
