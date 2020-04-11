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
		if (Node.InData.ConnectionReaction.ContainsKey(Connecting) && (!(ResistanceRestorepoints.ContainsKey(Connecting))))
		{
			ResistanceRestorepoints[Connecting] = Node.InData.ConnectionReaction[Connecting].ResistanceReactionA.Resistance.Ohms;
			Node.InData.ConnectionReaction[Connecting].ResistanceReactionA.Resistance.Ohms = InternalResistance;
		}
		ElectricalManager.Instance.electricalSync.InitialiseResistanceChange.Add(this);
	}

	public void RestoreResistance(PowerTypeCategory Connecting)
	{
		if (Node.InData.ConnectionReaction.ContainsKey(Connecting) && (ResistanceRestorepoints.ContainsKey(Connecting)))
		{
			Node.InData.ConnectionReaction[Connecting].ResistanceReactionA.Resistance.Ohms = ResistanceRestorepoints[Connecting];
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
			if (!UpdateRequestDictionary.ContainsKey(UpdateType))
			{
				UpdateRequestDictionary[UpdateType] = new HashSet<ElectricalModuleTypeCategory>();
			}
			UpdateRequestDictionary[UpdateType].Add(Module.ModuleType);
		}
	}

	public void UpDespawn(DespawnInfo info)
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.GoingOffStage))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.GoingOffStage])
			{
				UpdateDelegateDictionary[Module].OnDespawnServer(info);
			}
		}
	}

	public void UpOnStartServer()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.OnStartServer))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.OnStartServer])
			{
				UpdateDelegateDictionary[Module].OnStartServer();
			}
		}
	}

	public virtual void UpTurnOnSupply()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.TurnOnSupply))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.TurnOnSupply])
			{
				UpdateDelegateDictionary[Module].TurnOnSupply();
			}
		}
	}

	public virtual void UpTurnOffSupply()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.TurnOffSupply))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.TurnOffSupply])
			{
				UpdateDelegateDictionary[Module].TurnOffSupply();
			}
		}
	}

	public virtual void UpTurnOffCleanup()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.TurnOffCleanup))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.TurnOffCleanup])
			{
				UpdateDelegateDictionary[Module].TurnOffCleanup();
			}
		}
	}

	public void UpPowerUpdateStructureChange()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.PowerUpdateStructureChange))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.PowerUpdateStructureChange])
			{
				UpdateDelegateDictionary[Module].PowerUpdateStructureChange();
			}
		}
	}

	public void UpPowerUpdateStructureChangeReact()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.PowerUpdateStructureChangeReact))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.PowerUpdateStructureChangeReact])
			{
				UpdateDelegateDictionary[Module].PowerUpdateStructureChangeReact();
			}
		}
	}

	public void UpInitialPowerUpdateResistance()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.InitialPowerUpdateResistance))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.InitialPowerUpdateResistance])
			{
				UpdateDelegateDictionary[Module].InitialPowerUpdateResistance();
			}
		}
	}

	public void UpPowerUpdateResistanceChange()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.PowerUpdateResistanceChange))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.PowerUpdateResistanceChange])
			{
				UpdateDelegateDictionary[Module].PowerUpdateResistanceChange();
			}
		}
	}

	public void UpPowerUpdateCurrentChange()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.PowerUpdateCurrentChange))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.PowerUpdateCurrentChange])
			{
				UpdateDelegateDictionary[Module].PowerUpdateCurrentChange();
			}
		}
	}

	public void UpPotentialDestroyed()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.PotentialDestroyed))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.PotentialDestroyed])
			{
				UpdateDelegateDictionary[Module].PotentialDestroyed();
			}
		}
	}

	public void UpPowerNetworkUpdate()
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.PowerNetworkUpdate))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.PowerNetworkUpdate])
			{
				UpdateDelegateDictionary[Module].PowerNetworkUpdate();
			}
		}
	}


	public ResistanceWrap UpModifyResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.ModifyResistancyOutput))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.ModifyResistancyOutput])
			{
				Resistance = UpdateDelegateDictionary[Module].ModifyResistancyOutput(Resistance, SourceInstance);
			}
		}
		return (Resistance);
	}

	public ResistanceWrap UpModifyResistanceInput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom)
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.ModifyResistanceInput))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.ModifyResistanceInput])
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
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.ModifyElectricityInput))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.ModifyElectricityInput])
			{
				Current = UpdateDelegateDictionary[Module].ModifyElectricityInput(Current, SourceInstance, ComingFromm);
			}
		}
		return (Current);
	}

	public VIRCurrent UpModifyElectricityOutput(VIRCurrent Current, ElectricalOIinheritance SourceInstance)
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.ModifyElectricityOutput))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.ModifyElectricityOutput])
			{
				Current = UpdateDelegateDictionary[Module].ModifyElectricityOutput(Current, SourceInstance);
			}
		}
		return (Current);
	}


}