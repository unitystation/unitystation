using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ElectricalNodeControl : NetworkBehaviour, IServerDespawn
{
	[SerializeField]
	public InLineDevice Node;
	public PowerTypeCategory ApplianceType;
	public List<PowerTypeCategory> ListCanConnectTo;
	public HashSet<PowerTypeCategory> CanConnectTo;
	public bool SelfDestruct;
	public List<PowerInputReactions> Reactions;
	public Dictionary<PowerTypeCategory,float> ResistanceRestorepoints = new Dictionary<PowerTypeCategory, float>();

	public INodeControl NodeControl;

	public override void OnStartServer()
	{
		base.OnStartServer();
		NodeControl = gameObject.GetComponent<INodeControl>();
		Node = gameObject.GetComponent<InLineDevice>();
		CanConnectTo = new HashSet<PowerTypeCategory>(ListCanConnectTo);
		Node.InData.Categorytype = ApplianceType;
		Node.InData.CanConnectTo = CanConnectTo;
		Node.InData.ControllingDevice = this;
		foreach (PowerInputReactions ReactionC in Reactions)
		{
			Node.InData.ConnectionReaction[ReactionC.ConnectingDevice] = ReactionC;
		}
		gameObject.SendMessage("BroadcastSetUpMessage", this, SendMessageOptions.DontRequireReceiver);
		UpOnStartServer();
		StartCoroutine(WaitForload());
		ElectricalSynchronisation.StructureChange = true;
	}

	IEnumerator WaitForload()
	{
		yield return WaitFor.Seconds(1);
		Node.FindPossibleConnections();
		Node.FlushConnectionAndUp();
	}

	public void PotentialDestroyed()
	{
		UpPotentialDestroyed();
		if (SelfDestruct)
		{
			ElectricalSynchronisation.RemoveSupply(this, ApplianceType);
			Despawn.ServerSingle(gameObject);
		}

	}
	public void OverlayInternalResistance(float InternalResistance, PowerTypeCategory Connecting) {
		if (Node.InData.ConnectionReaction.ContainsKey(Connecting) && (!(ResistanceRestorepoints.ContainsKey(Connecting))))
		{
			ResistanceRestorepoints[Connecting] = Node.InData.ConnectionReaction[Connecting].ResistanceReactionA.Resistance.Ohms;
			Node.InData.ConnectionReaction[Connecting].ResistanceReactionA.Resistance.Ohms = InternalResistance;
		}
		ElectricalSynchronisation.InitialiseResistanceChange.Add(this);

	}
	public void RestoreResistance(PowerTypeCategory Connecting) {
		if (Node.InData.ConnectionReaction.ContainsKey(Connecting) && (ResistanceRestorepoints.ContainsKey(Connecting)))
		{
			Node.InData.ConnectionReaction[Connecting].ResistanceReactionA.Resistance.Ohms = ResistanceRestorepoints[Connecting];
			ResistanceRestorepoints.Remove(Connecting);
		}
	}


	/// <summary>
	/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
	/// </summary>
	public void OnDespawnServer(DespawnInfo info) {
		Node.FlushConnectionAndUp();
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

	public  void PowerUpdateStructureChange()
	{
		UpPowerUpdateStructureChange();
	}
	public void PowerUpdateStructureChangeReact()
	{
		UpPowerUpdateStructureChangeReact();
	}

	public  void InitialPowerUpdateResistance()
	{
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in Node.Data.ResistanceToConnectedDevices)
		{
			Node.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
		}
		UpInitialPowerUpdateResistance();
	}

	public  void PowerUpdateResistanceChange()
	{
		foreach (KeyValuePair<ElectricalOIinheritance, HashSet<PowerTypeCategory>> Supplie in Node.Data.ResistanceToConnectedDevices)
		{
			Node.ResistanceInput(1.11111111f, Supplie.Key.GameObject(), null);
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingDevice);
		}
		UpPowerUpdateResistanceChange();
	}


	public void PowerUpdateCurrentChange()
	{
		ElectricityFunctions.WorkOutActualNumbers(Node);
		UpPowerUpdateCurrentChange();
	}

	public void PowerNetworkUpdate()
	{
		ElectricityFunctions.WorkOutActualNumbers(Node);
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


	public  float ModifyElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		return (UpModifyElectricityInput(Current, SourceInstance, ComingFrom));
	}
	public  float ModifyElectricityOutput(float Current, GameObject SourceInstance)
	{
		return (UpModifyElectricityOutput(Current, SourceInstance));
	}

	public  float ModifyResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		return (UpModifyResistanceInput(Resistance, SourceInstance, ComingFrom));
	}
	public  float ModifyResistancyOutput(float Resistance, GameObject SourceInstance)
	{
		return (UpModifyResistancyOutput(Resistance,SourceInstance));
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

	public void UpDespawn(DespawnInfo info) {
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


	public float UpModifyResistancyOutput(float Resistance, GameObject SourceInstance)
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

	public float UpModifyResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
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


	public float UpModifyElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom)
	{
		if (UpdateRequestDictionary.ContainsKey(ElectricalUpdateTypeCategory.ModifyElectricityInput))
		{
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.ModifyElectricityInput])
			{
				Current = UpdateDelegateDictionary[Module].ModifyElectricityInput(Current, SourceInstance, ComingFrom);
			}
		}
		return (Current);
	}

	public float UpModifyElectricityOutput(float Current, GameObject SourceInstance)
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