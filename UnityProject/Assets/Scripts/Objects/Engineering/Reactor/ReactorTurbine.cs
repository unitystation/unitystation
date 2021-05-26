﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Electricity.NodeModules;

namespace Objects.Engineering
{
	public class ReactorTurbine : MonoBehaviour, INodeControl, ISetMultitoolSlave, ISetMultitoolMaster, ICheckedInteractable<HandApply>
	{
		public ModuleSupplyingDevice moduleSupplyingDevice;
		public GameObject ConstructMaterial;
		[SerializeField] private int droppedMaterialAmount = 25;
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
				moduleSupplyingDevice.ProducingWatts = (float)Boiler.OutputEnergy;
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

			if (!DefaultWillInteract.Default(interaction, side)) return false;
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
		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.BoilerTurbine;
		public MultitoolConnectionType ConType => conType;

		private bool multiMaster = false;
		public bool MultiMaster => multiMaster;

		public void SetMaster(ISetMultitoolMaster Imaster)
		{
			var boiler = (Imaster as Component)?.gameObject.GetComponent<ReactorBoiler>();
			if (boiler != null)
			{
				Boiler = boiler;
			}
		}

		public void AddSlave(object SlaveObjectThis) { }

		#endregion
	}
}
