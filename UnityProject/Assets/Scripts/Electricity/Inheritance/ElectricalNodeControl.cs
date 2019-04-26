using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElectricalNodeControl : NetworkBehaviour
{
	[SerializeField]
	public InLineDevice2 Node;
	public ElectricalOIinheritance _IElectricityIO;
	public PowerTypeCategory ApplianceType;
	public List<PowerTypeCategory> ListCanConnectTo;
	public HashSet<PowerTypeCategory> CanConnectTo;

	public List<PowerInputReactions> Reactions;
	public override void OnStartServer()
	{
		base.OnStartServer();
		BroadcastMessage("BroadcastSetUpMessage", this);
		ElectricalSynchronisation.StructureChange = true;
		CanConnectTo = new HashSet<PowerTypeCategory>(ListCanConnectTo);
		_IElectricityIO = Node;
		Node.InData.Categorytype = ApplianceType;
		Node.InData.CanConnectTo = CanConnectTo;
		Node.RelatedDevice = this;//fix line inheritance
		foreach (PowerInputReactions ReactionC in Reactions)
		{
			Node.InData.ConnectionReaction[ReactionC.ConnectingDevice] = ReactionC;
		}
		UpOnStartServer();

		//Node.InData.ControllingDevice = this;
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
		Node.FlushConnectionAndUp();
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
			ElectricalSynchronisation.NUCurrentChange.Add(Supplie.Key.InData.ControllingUpdate);
		}
		UpInitialPowerUpdateResistance();
	}

	public  void PowerUpdateResistanceChange()
	{
		UpPowerUpdateResistanceChange();
	}

	public  void PowerNetworkUpdate()
	{
		UpPowerNetworkUpdate();
	}

	public  void PowerUpdateCurrentChange()
	{
		UpPowerUpdateCurrentChange();
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
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.OnStartServer])
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
			foreach (ElectricalModuleTypeCategory Module in UpdateRequestDictionary[ElectricalUpdateTypeCategory.OnStartServer])
			{
				UpdateDelegateDictionary[Module].PowerUpdateCurrentChange();
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