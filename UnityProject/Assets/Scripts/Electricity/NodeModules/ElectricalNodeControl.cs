using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ElectricalNodeControl : NetworkBehaviour, IServerDespawn
{
	[SerializeField]
	public InLineDevice Node;
	public PowerTypeCategory ApplianceType;
	public Connection WireEndB;
	public Connection WireEndA;
	public List<PowerTypeCategory> ListCanConnectTo;
	public HashSet<PowerTypeCategory> CanConnectTo;
	public bool SelfDestruct;
	public List<PowerInputReactions> Reactions;
	public Dictionary<PowerTypeCategory, float> ResistanceRestorepoints = new Dictionary<PowerTypeCategory, float>();

	public INodeControl NodeControl;

	public override void OnStartServer()
	{
		//Logger.Log("yoooooo");
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
		UpOnStartServer();
		ElectricalManager.Instance.electricalSync.StructureChange = true;
	}

	void Start()
	{
		ElectricalManager.Instance.electricalSync.StructureChange = true;
	}

	public void PotentialDestroyed()
	{
		UpPotentialDestroyed();
		if (SelfDestruct)
		{
			ElectricalManager.Instance.electricalSync.RemoveSupply(this, ApplianceType);
			Despawn.ServerSingle(gameObject);
		}
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

	/// <summary>
	/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
	/// </summary>
	public void OnDespawnServer(DespawnInfo info)
	{
		Node.InData.FlushConnectionAndUp();
		UpDespawn(info);
	}

	public void TurnOnSupply()
	{
		UpTurnOnSupply();
	}
	public void TurnOffSupply()
	{
		UpTurnOffSupply();
	}

	public void PowerUpdateStructureChange()
	{
		UpPowerUpdateStructureChange();
	}
	public void PowerUpdateStructureChangeReact()
	{
		UpPowerUpdateStructureChangeReact();
	}

	public void InitialPowerUpdateResistance()
	{
		UpInitialPowerUpdateResistance();
	}

	public void PowerUpdateResistanceChange()
	{
		UpPowerUpdateResistanceChange();
	}


	public void PowerUpdateCurrentChange()
	{
		ElectricityFunctions.WorkOutActualNumbers(Node.InData);
		UpPowerUpdateCurrentChange();
	}

	public void PowerNetworkUpdate()
	{
		ElectricityFunctions.WorkOutActualNumbers(Node.InData);
		UpPowerNetworkUpdate();
		if (NodeControl != null)
		{
			NodeControl.PowerNetworkUpdate();
		}
	}

	public void TurnOffCleanup()
	{
		UpTurnOffCleanup();
	}


	public VIRCurrent ModifyElectricityInput(VIRCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 IntrinsicElectronicData ComingFromm)
	{
		return (UpModifyElectricityInput(Current, SourceInstance, ComingFromm));
	}
	public VIRCurrent ModifyElectricityOutput(VIRCurrent Current, ElectricalOIinheritance SourceInstance)
	{
		return (UpModifyElectricityOutput(Current, SourceInstance));
	}

	public ResistanceWrap ModifyResistanceInput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom)
	{
		return (UpModifyResistanceInput(Resistance, SourceInstance, ComingFrom));
	}
	public ResistanceWrap ModifyResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
	{
		return (UpModifyResistancyOutput(Resistance, SourceInstance));
	}




	//Update manager
	public Dictionary<ElectricalModuleTypeCategory, ElectricalModuleInheritance> UpdateDelegateDictionary =
		new Dictionary<ElectricalModuleTypeCategory, ElectricalModuleInheritance>();

	public Dictionary<ElectricalUpdateTypeCategory, HashSet<ElectricalModuleTypeCategory>> UpdateRequestDictionary =
		new Dictionary<ElectricalUpdateTypeCategory, HashSet<ElectricalModuleTypeCategory>>();

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

	public void UpDespawn(DespawnInfo info)
	{

		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.GoingOffStage, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].OnDespawnServer(info);
			}
		}
	}

	public void UpOnStartServer()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.OnStartServer, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].OnStartServer();
			}
		}
	}

	public virtual void UpTurnOnSupply()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.TurnOnSupply, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].TurnOnSupply();
			}
		}
	}

	public virtual void UpTurnOffSupply()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.TurnOffSupply, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].TurnOffSupply();
			}
		}
	}

	public virtual void UpTurnOffCleanup()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.TurnOffCleanup, out var updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].TurnOffCleanup();
			}
		}
	}

	public void UpPowerUpdateStructureChange()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateStructureChange, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].PowerUpdateStructureChange();
			}
		}
	}

	public void UpPowerUpdateStructureChangeReact()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateStructureChangeReact, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].PowerUpdateStructureChangeReact();
			}
		}
	}

	public void UpInitialPowerUpdateResistance()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.InitialPowerUpdateResistance, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].InitialPowerUpdateResistance();
			}
		}
	}

	public void UpPowerUpdateResistanceChange()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateResistanceChange, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].PowerUpdateResistanceChange();
			}
		}
	}

	public void UpPowerUpdateCurrentChange()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerUpdateCurrentChange, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].PowerUpdateCurrentChange();
			}
		}
	}

	public void UpPotentialDestroyed()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PotentialDestroyed, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].PotentialDestroyed();
			}
		}
	}

	public void UpPowerNetworkUpdate()
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.PowerNetworkUpdate, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				UpdateDelegateDictionary[Module].PowerNetworkUpdate();
			}
		}
	}


	public ResistanceWrap UpModifyResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyResistancyOutput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				Resistance = UpdateDelegateDictionary[Module].ModifyResistancyOutput(Resistance, SourceInstance);
			}
		}
		return (Resistance);
	}

	public ResistanceWrap UpModifyResistanceInput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom)
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyResistanceInput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				Resistance = UpdateDelegateDictionary[Module].ModifyResistanceInput(Resistance, SourceInstance, ComingFrom);
			}
		}
		return (Resistance);
	}


	public VIRCurrent UpModifyElectricityInput(VIRCurrent Current,
										 ElectricalOIinheritance SourceInstance,
										 IntrinsicElectronicData ComingFromm)
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyElectricityInput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				Current = UpdateDelegateDictionary[Module].ModifyElectricityInput(Current, SourceInstance, ComingFromm);
			}
		}
		return (Current);
	}

	public VIRCurrent UpModifyElectricityOutput(VIRCurrent Current, ElectricalOIinheritance SourceInstance)
	{
		if (UpdateRequestDictionary.TryGetValue(ElectricalUpdateTypeCategory.ModifyElectricityOutput, out HashSet<ElectricalModuleTypeCategory> updateRequest))
		{
			foreach (ElectricalModuleTypeCategory Module in updateRequest)
			{
				Current = UpdateDelegateDictionary[Module].ModifyElectricityOutput(Current, SourceInstance);
			}
		}
		return (Current);
	}
}