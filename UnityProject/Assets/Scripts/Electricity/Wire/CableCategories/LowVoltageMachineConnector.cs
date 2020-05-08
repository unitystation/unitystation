using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class LowVoltageMachineConnector : NetworkBehaviour  , ICheckedInteractable<PositionalHandApply>
{
	private bool SelfDestruct = false;

	public WireConnect RelatedWire;
	public PowerTypeCategory ApplianceType = PowerTypeCategory.LowMachineConnector;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.LowVoltageCable,
		PowerTypeCategory.APC,
	};

	public void PotentialDestroyed(){
		if (SelfDestruct) {
			
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		RelatedWire.InData.CanConnectTo = CanConnectTo;
		RelatedWire.InData.Categorytype = ApplianceType;
		RelatedWire.InData.WireEndA = Connection.MachineConnect;
		RelatedWire.InData.WireEndB = Connection.Overlap;
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return false;
		if (interaction.TargetObject != gameObject) return false;
		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//wirecutters can be used to cut this cable
		Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
		MatrixInfo matrix = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);

		if (matrix.Matrix == null || !matrix.Matrix.IsClearUnderfloorConstruction(localPosInt, true))
		{
			return;
		}

		Spawn.ServerPrefab("Low machine connector", gameObject.AssumedWorldPosServer());
		Despawn.ServerSingle(gameObject);
	}
}

