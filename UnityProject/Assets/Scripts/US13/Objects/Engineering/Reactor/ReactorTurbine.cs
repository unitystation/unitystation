using System;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.ObjectConnection;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Electricity.NodeModules;
using Util;

namespace US13.Objects.Engineering.Reactor
{
	public class ReactorTurbine : MonoBehaviour, INodeControl, IMultitoolSlaveable, IMultitoolMasterable, ICheckedInteractable<HandApply>, IServerSpawn
	{
		public ModuleSupplyingDevice moduleSupplyingDevice;
		public GameObject ConstructMaterial;
		[SerializeField]
		private int droppedMaterialAmount = 25;
		public ReactorBoiler Boiler;
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = false;

		public event Action<PowerState, PowerState> OnStateChangeEvent;
		private PowerState currentPowerState = PowerState.Off;

		private int lowWattageThreshold = 25000;
		private int highWattageThreshold = 5000000;

		#region Lifecycle

		private void Start()
		{
			moduleSupplyingDevice = GetComponent<ModuleSupplyingDevice>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			moduleSupplyingDevice?.TurnOnSupply();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CycleUpdate, 1);
			//moduleSupplyingDevice = this.GetComponent<ModuleSupplyingDevice>();

		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
			moduleSupplyingDevice?.TurnOffSupply();
		}

		#endregion

		public void CycleUpdate()
		{
			if (Boiler != null)
			{
				moduleSupplyingDevice.ProducingWatts  = (moduleSupplyingDevice.ProducingWatts  + (float)Boiler.OutputEnergy) / 2;
			}
			else
			{
				moduleSupplyingDevice.ProducingWatts = 0;
			}

		}

		void INodeControl.PowerNetworkUpdate()
		{
			SetPowerStateFromVoltage();
		}
		public PowerState SetPowerStateFromVoltage()
		{
			PowerState newState = currentPowerState;

			if (moduleSupplyingDevice.ProducingWatts == 0) newState = PowerState.Off;
			else if (moduleSupplyingDevice.ProducingWatts <= lowWattageThreshold) newState = PowerState.LowVoltage;
			else if (moduleSupplyingDevice.ProducingWatts >= highWattageThreshold) newState = PowerState.OverVoltage;
			else newState = PowerState.On;

			if (newState == currentPowerState) return currentPowerState;
			OnStateChangeEvent?.Invoke(currentPowerState, newState);
			currentPowerState = newState;
			return currentPowerState;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{

			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (!Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 10,
					"You start to deconstruct the ReactorTurbine..",
					$"{interaction.Performer.ExpensiveName()} starts to deconstruct the ReactorTurbine...",
					"You deconstruct the ReactorTurbine",
					$"{interaction.Performer.ExpensiveName()} deconstruct the ReactorTurbine.",
					() =>
					{
						Spawn.ServerPrefab(ConstructMaterial, gameObject.AssumedWorldPosServer(), count: droppedMaterialAmount); //Spawning plates here as OnDespawnServer gets derailed by the electricity code
						_ = Despawn.ServerSingle(gameObject);
					});
			}
		}

		/// <summary>
		/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
		/// </summary>
		///
		//public void OnDespawnServer(DespawnInfo info)
		//{
		//	Spawn.ServerPrefab(ConstructMaterial, gameObject.AssumedWorldPosServer(), count: droppedMaterialAmount);
		//}
		/* OnDespawnServer was non-functional.
		 * It still fires, however the electrical code resets the position so it's spawned in the shadow realm.
		 * If you get it to work you're better than I am.
		 */

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.BoilerTurbine;

		// Master connection
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		// Slave connection
		IMultitoolMasterable IMultitoolSlaveable.Master => Boiler;
		bool IMultitoolSlaveable.RequireLink => true;
		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}
		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			Boiler = master is ReactorBoiler boiler ? boiler : null;
		}

		#endregion
	}
}
