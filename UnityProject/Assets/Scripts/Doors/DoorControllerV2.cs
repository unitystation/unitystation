using System;
using Doors.DoorFSM;
using Mirror;
using NaughtyAttributes;
using UnityEngine;

namespace Doors
{
	/// <summary>
	/// Generic class for handling base door interactions.
	/// </summary>
	public class DoorControllerV2: MonoBehaviour, IServerSpawn, IExaminable, IServerDespawn
	{
		#region Inspector
		[SerializeField,
		 Tooltip("Is this door designed no matter what is under neath it?")]
		private bool ignorePassableChecks = false;

		[SerializeField,
		 Tooltip("Does this door open automatically when you walk into it?")]
		private bool isAutomatic = true;

		[SerializeField,
		 Tooltip("How much time in seconds will this door keep open until automatically closed. " +
		         "Negative numbers will disable auto-close"),
		 ShowIf(nameof(isAutomatic))]
		private float autoCloseTime = 5;

		[SerializeField,
		 Tooltip("Should this door spawn with bolts down?")]
		private bool hasBoltsDown = false;

		[SerializeField,
		 Tooltip("Should this door spawn with the bolts light enabled?")]
		private bool hasBoltsLights = true;

		[SerializeField,
		 Tooltip("Should this door spawn with the maintenance panel exposed?")]
		private bool hasPanelExposed = false;

		[SerializeField,
		 Tooltip("Should this door play a pressure warning when there are dangerous difference in pressure?")]
		private bool doesPressureWarning = false;

		#endregion

		private const string OPEN_DOOR_LAYER = "Door Open";
		private StateMachine doorFSM;
		private DoorProperties doorProperties;
		private APCPoweredDevice poweredDevice;

		public bool IsClosed => doorFSM.RegisterDoor.IsClosed;
		public bool HasPanelExposed => doorProperties.HasPanelExposed;

		private void Awake()
		{
			poweredDevice = GetComponent<APCPoweredDevice>();
		}


		public void Bump(GameObject byPlayer)
		{
			if (isAutomatic)
			{
				TryOpen(byPlayer);
			}
		}


		public void OnSpawnServer(SpawnInfo info)
		{
			doorProperties = new DoorProperties()
			{
				IsAutomatic = isAutomatic,
				AutoCloseTime = autoCloseTime,
				HasBoltsDown = hasBoltsDown,
				HasBoltLights = hasBoltsLights,
				HasPanelExposed = hasPanelExposed,
				DoesPressureWarning =  doesPressureWarning
			};

			doorProperties.HasPower = (poweredDevice.State == PowerStates.On);
			doorFSM = new StateMachine(gameObject, doorProperties);
			poweredDevice.PowerStateChangedEvent += ChangePower;
		}


		public void OnDespawnServer(DespawnInfo info)
		{
			poweredDevice.PowerStateChangedEvent -= ChangePower;
		}


		#region interface methods
		public void TryOpen(GameObject byPlayer, bool force = false)
		{
			if (!force && !doorFSM.AccessRestrictions.CheckAccess(byPlayer))
			{
				StartCoroutine(doorFSM.DoorAnimatorV2.PlayDeniedAnimation());
				return;
			}

			doorFSM.NextState(doorFSM.CurrentState.TryOpen(byPlayer, force));
		}

		public void TryClose(GameObject byPlayer)
		{
			doorFSM.NextState(doorFSM.CurrentState.TryClose(byPlayer));
		}

		public void TryBolts(GameObject byPlayer)
		{
			doorFSM.NextState(doorFSM.CurrentState.TryBolts(byPlayer));
		}

		public void TryBoltLights(GameObject byPlayer)
		{
			doorFSM.NextState(doorFSM.CurrentState.TryBoltLights(byPlayer));
		}

		public void TryWelder(Intent intent, GameObject byPlayer)
		{
			doorFSM.NextState(doorFSM.CurrentState.TryWelder(intent, byPlayer));
		}

		public void TryPanel(GameObject byPlayer)
		{
			doorFSM.NextState(doorFSM.CurrentState.TryPanel(byPlayer));
		}

		public void TryCrowbar(GameObject byPlayer)
		{
			doorFSM.NextState(doorFSM.CurrentState.TryCrowbar(byPlayer));
		}

		public void TryEmag()
		{
			doorFSM.NextState(doorFSM.CurrentState.TryEmag(doorFSM.CurrentState));
		}

		public void TryReplaceCB(GameObject byPlayer, GameObject newCircuitBoard)
		{
			doorFSM.NextState(doorFSM.CurrentState.TryReplaceCB(byPlayer, newCircuitBoard));
		}

		private void ChangePower(PowerStates state)
		{
			doorFSM.NextState(doorFSM.CurrentState.ChangePower(state));
		}
		#endregion


		public string Examine(Vector3 worldPos = default(Vector3))
		{
			string examineMessage = $"The {gameObject.ExpensiveName()} is ";

			if (!IsClosed)
			{
				examineMessage += "open. ";
			}
			else
			{
				examineMessage += "closed. ";
			}

			if (doorFSM.Properties.IsWeld)
			{
				examineMessage += "Seems like someone welded it. ";
			}

			if (doorFSM.Properties.HasPanelExposed)
			{
				examineMessage += "The maintenance panel is open and it has all the wiring exposed. ";
			}

			if (doorFSM.Properties.HasPower && doorFSM.Properties.HasBoltsDown && doorFSM.Properties.HasBoltLights)
			{
				examineMessage += "The bolts lights are on. ";
			}

			if (!doorFSM.Properties.HasPower)
			{
				examineMessage += "Doesn't look like it is powered. ";
			}

			return examineMessage;
		}

		public void UpdateNewPlayer(NetworkConnection connection)
		{
			//TODO update the current door state to this particular player
			throw new NotImplementedException();
		}
	}
}