using Shared.Systems.ObjectConnection;
using UnityEngine;
using Systems.Electricity.NodeModules;

namespace Objects.Engineering
{
	public class ReactorTurbine : MonoBehaviour, INodeControl, IMultitoolSlaveable, IMultitoolMasterable, ICheckedInteractable<HandApply>
	{
		public ModuleSupplyingDevice moduleSupplyingDevice;
		public GameObject ConstructMaterial;
		[SerializeField]
		private int droppedMaterialAmount = 25;
		public ReactorBoiler Boiler;

		#region Lifecycle

		private void Start()
		{
			moduleSupplyingDevice = GetComponent<ModuleSupplyingDevice>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.Instance._isServer == false) return;

			UpdateManager.Add(CycleUpdate, 1);
			//moduleSupplyingDevice = this.GetComponent<ModuleSupplyingDevice>();
			moduleSupplyingDevice?.TurnOnSupply();
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.Instance._isServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
			moduleSupplyingDevice?.TurnOffSupply();
		}

		#endregion

		public void CycleUpdate()
		{
			if (Boiler != null)
			{
				//Logger.Log("  moduleSupplyingDevice.ProducingWatts " +   moduleSupplyingDevice.ProducingWatts);
				moduleSupplyingDevice.ProducingWatts  = moduleSupplyingDevice.ProducingWatts  + ((float)Boiler.OutputEnergy) / 2;
			}
			else
			{
				moduleSupplyingDevice.ProducingWatts = 0;
			}

		}

		void INodeControl.PowerNetworkUpdate()
		{
			//Stuff for the senses to read
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
		bool IMultitoolMasterable.MultiMaster => false;
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
