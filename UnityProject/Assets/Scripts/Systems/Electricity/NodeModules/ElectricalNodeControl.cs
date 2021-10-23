using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Electricity;
using Systems.Electricity.Inheritance;
using UnityEngine;
using Mirror;
using NaughtyAttributes;

namespace Systems.Electricity.NodeModules
{
	public class ElectricalNodeControl : NetworkBehaviour, IServerDespawn
	{
		[SerializeField]
		public InLineDevice Node;
		public PowerTypeCategory ApplianceType;
		public Connection WireEndB;
		public Connection WireEndA;
		public List<PowerTypeCategory> ListCanConnectTo;
		[NaughtyAttributes.ReadOnlyAttribute] public HashSet<PowerTypeCategory> CanConnectTo;
		public List<PowerInputReactions> Reactions;
		public Dictionary<PowerTypeCategory, float> ResistanceRestorepoints = new Dictionary<PowerTypeCategory, float>();

		[NaughtyAttributes.ReadOnlyAttribute] public INodeControl NodeControl;

		// Update manager
		public Dictionary<ElectricalModuleTypeCategory, ElectricalModuleInheritance> UpdateDelegateDictionary =
			new Dictionary<ElectricalModuleTypeCategory, ElectricalModuleInheritance>();

		public Dictionary<ElectricalUpdateTypeCategory, HashSet<ElectricalModuleTypeCategory>> UpdateRequestDictionary =
			new Dictionary<ElectricalUpdateTypeCategory, HashSet<ElectricalModuleTypeCategory>>();

		#region Lifecycle

		void Start()
		{
			ElectricalManager.Instance.electricalSync.StructureChange = true;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			NodeControl = gameObject.GetComponent<INodeControl>();
			Node = gameObject.GetComponent<InLineDevice>();
			CanConnectTo = new HashSet<PowerTypeCategory>(ListCanConnectTo);
			Node.InData.Categorytype = ApplianceType;
			Node.InData.CanConnectTo = CanConnectTo;
			Node.InData.WireEndA = WireEndA;
			Node.InData.WireEndB = WireEndB;
			Node.InData.ControllingDevice = this;
			foreach (PowerInputReactions ReactionC in Reactions)
			{
				Node.InData.ConnectionReaction[ReactionC.ConnectingDevice] = ReactionC;
			}
			gameObject.SendMessage("BroadcastSetUpMessage", this, SendMessageOptions.DontRequireReceiver);
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.OnStartServer, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].OnStartServer();
				}
			}
			ElectricalManager.Instance.electricalSync.StructureChange = true;
		}

		/// <summary>
		/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
		/// </summary>
		public void OnDespawnServer(DespawnInfo info)
		{
			Node.InData.FlushConnectionAndUp();
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.GoingOffStage, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].OnDespawnServer(info);
				}
			}

		}

		#endregion

		public float GetVoltage()
		{
			ElectricityFunctions.WorkOutActualNumbers(Node.InData);
			return Node.InData.Data.ActualVoltage;
		}

		public float GetCurrent()
		{
			ElectricityFunctions.WorkOutActualNumbers(Node.InData);
			return Node.InData.Data.CurrentInWire;
		}

		public float GetResistance()
		{
			ElectricityFunctions.WorkOutActualNumbers(Node.InData);
			return Node.InData.Data.EstimatedResistance;
		}

		public void OverlayInternalResistance(float InternalResistance, PowerTypeCategory Connecting)
		{
			if (Node.InData.ConnectionReaction.TryGetValue(Connecting, out PowerInputReactions connReact) && !ResistanceRestorepoints.ContainsKey(Connecting))
			{
				ResistanceRestorepoints[Connecting] = connReact.ResistanceReactionA.Resistance.Ohms;
				connReact.ResistanceReactionA.Resistance.Ohms = InternalResistance;
			}
			ElectricalManager.Instance.electricalSync.InitialiseResistanceChange.Add(this);
		}

		public void RestoreResistance(PowerTypeCategory Connecting)
		{

			if (Node.InData.ConnectionReaction.TryGetValue(Connecting, out PowerInputReactions connReact) && ResistanceRestorepoints.TryGetValue(Connecting, out float restore))
			{
				connReact.ResistanceReactionA.Resistance.Ohms = restore;
				ResistanceRestorepoints.Remove(Connecting);
			}
		}

		public void PowerNetworkUpdate()
		{
			ElectricityFunctions.WorkOutActualNumbers(Node.InData);
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerNetworkUpdate, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].PowerNetworkUpdate();
				}
			}
			if (NodeControl != null)
			{
				NodeControl.PowerNetworkUpdate();
			}
		}

		public void AddModule(ElectricalModuleInheritance Module)
		{
			UpdateDelegateDictionary[Module.ModuleType] = Module;
			foreach (ElectricalUpdateTypeCategory UpdateType in Module.RequiresUpdateOn)
			{
				if (UpdateRequestDictionary.TryGetValue(UpdateType, out var updateRequest))
				{
					updateRequest.Add(Module.ModuleType);
				}
				else
				{
					UpdateRequestDictionary[UpdateType] = new HashSet<ElectricalModuleTypeCategory>() { Module.ModuleType };
				}
			}
		}

		public virtual void TurnOnSupply()
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.TurnOnSupply, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].TurnOnSupply();
				}
			}
		}

		public virtual void TurnOffSupply()
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.TurnOffSupply, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].TurnOffSupply();
				}
			}
		}

		public virtual void TurnOffCleanup()
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.TurnOffCleanup, out var updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].TurnOffCleanup();
				}
			}
		}

		public void PowerUpdateStructureChange()
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateStructureChange, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].PowerUpdateStructureChange();
				}
			}
		}

		public void PowerUpdateStructureChangeReact()
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateStructureChangeReact, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].PowerUpdateStructureChangeReact();
				}
			}
		}

		public void InitialPowerUpdateResistance()
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.InitialPowerUpdateResistance, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].InitialPowerUpdateResistance();
				}
			}
		}

		public void PowerUpdateResistanceChange()
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateResistanceChange, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].PowerUpdateResistanceChange();
				}
			}
		}

		public void PowerUpdateCurrentChange()
		{
			ElectricityFunctions.WorkOutActualNumbers(Node.InData);
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateCurrentChange, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					UpdateDelegateDictionary[Module].PowerUpdateCurrentChange();
				}
			}
		}

		public ResistanceWrap ModifyResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyResistancyOutput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					Resistance = UpdateDelegateDictionary[Module].ModifyResistancyOutput(Resistance, SourceInstance);
				}
			}
			return Resistance;
		}

		public ResistanceWrap ModifyResistanceInput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom)
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyResistanceInput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					Resistance = UpdateDelegateDictionary[Module].ModifyResistanceInput(Resistance, SourceInstance, ComingFrom);
				}
			}
			return Resistance;
		}

		public VIRCurrent ModifyElectricityInput(VIRCurrent Current, ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFromm)
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyElectricityInput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					Current = UpdateDelegateDictionary[Module].ModifyElectricityInput(Current, SourceInstance, ComingFromm);
				}
			}
			return Current;
		}

		public VIRCurrent ModifyElectricityOutput(VIRCurrent Current, ElectricalOIinheritance SourceInstance)
		{
			if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyElectricityOutput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
			{
				foreach (ElectricalModuleTypeCategory Module in updateRequest)
				{
					Current = UpdateDelegateDictionary[Module].ModifyElectricityOutput(Current, SourceInstance);
				}
			}
			return Current;
		}
	}
}
