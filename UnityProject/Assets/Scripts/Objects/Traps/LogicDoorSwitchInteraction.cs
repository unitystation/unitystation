using UnityEngine;
using Objects.Traps;
using Systems.Clearance;
using Objects.Wallmounts;
using Shared.Systems.ObjectConnection;
using Systems.Electricity;

namespace Objects.Logic
{
	[RequireComponent(typeof(DoorSwitch))]
	[RequireComponent(typeof(ClearanceRestricted))]
	public class LogicDoorSwitchInteraction : MonoBehaviour, IGenericTrigger, IMultitoolLinkable
	{
		private DoorSwitch doorSwitch = null;
		private ClearanceRestricted clearanceRestricted = null;
		public TriggerType TriggerType { get; protected set; } = TriggerType.Active;

		protected void Awake()
		{
			clearanceRestricted = GetComponent<ClearanceRestricted>();
			doorSwitch = GetComponent<DoorSwitch>();
		}

		public void OnTriggerWithClearance(IClearanceSource source)
		{
			if (clearanceRestricted.HasClearance(source) == false)
			{
				doorSwitch.RpcPlayButtonAnim(false);
				return;
			}

			if (PerformChecks() == false) return;

			doorSwitch.RpcPlayButtonAnim(true);
			doorSwitch.OpenDoors();
		}

		public void OnTriggerEnd()
		{
			if (PerformChecks() == false) return;

			doorSwitch.RpcPlayButtonAnim(true);
			doorSwitch.CloseDoors();
		}

		public bool PerformChecks()
		{
			if(doorSwitch.TestCoolDown() == false) return false;

			if (doorSwitch.NewDoorCount == 0) return false;
			if (doorSwitch.thisAPCPoweredDevice != null && APCPoweredDevice.IsOn(doorSwitch.thisAPCPoweredDevice.State) == false) return false;

			return true;
		}


		[SerializeField] private MultitoolConnectionType conType = MultitoolConnectionType.GenericTrigger;
		public MultitoolConnectionType ConType => conType;

		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = true;
	}
}
