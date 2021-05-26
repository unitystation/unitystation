using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;

namespace Objects.Engineering
{
	public class LowVoltageMachineConnector : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		[Tooltip("The machine connector prefab to spawn on interaction.")]
		[SerializeField]
		private GameObject machineConnectorPrefab = default;

		public WireConnect RelatedWire;
		public PowerTypeCategory ApplianceType = PowerTypeCategory.LowMachineConnector;
		public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.DepartmentBattery,
		PowerTypeCategory.LowVoltageCable,
		PowerTypeCategory.APC,
	};

		public override void OnStartServer()
		{
			base.OnStartServer();
			RelatedWire.InData.CanConnectTo = CanConnectTo;
			RelatedWire.InData.Categorytype = ApplianceType;
			RelatedWire.InData.WireEndA = Connection.MachineConnect;
			RelatedWire.InData.WireEndB = Connection.SurroundingTiles;
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter) == false) return false;
			if (interaction.TargetObject != gameObject) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			// wirecutters can be used to cut this cable
			Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
			var matrixInfo = MatrixManager.AtPoint(worldPosInt, true);
			var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixInfo);
			var matrix = matrixInfo?.Matrix;

			if (matrix == null || matrix.IsClearUnderfloorConstruction(localPosInt, true) == false) return;

			ToolUtils.ServerPlayToolSound(interaction);

			Spawn.ServerPrefab(machineConnectorPrefab, gameObject.AssumedWorldPosServer());
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
