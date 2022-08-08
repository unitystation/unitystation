using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;

namespace Objects.Engineering
{
	public class LowVoltageMachineConnector : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>, IServerSpawn
	{
		[Tooltip("The machine connector prefab to spawn on interaction.")]
		[SerializeField]
		private GameObject machineConnectorPrefab = default;

		[SerializeField] private float progressTimeToRemove = 1f;
		[SyncVar] private bool removedOnce = false;

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


		public void OnSpawnServer(SpawnInfo info)
		{
			removedOnce = false; //(Max) : Bod said this was important for object pooling.
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
			void Remove()
			{
				if (removedOnce == false) Spawn.ServerPrefab(
					machineConnectorPrefab, gameObject.AssumedWorldPosServer(),
					// Random positioning to make it clear this is disassembled
					scatterRadius: 0.35f, localRotation: RandomUtils.RandomRotation2D());
				removedOnce = true; //Counter measure incase the gameobject doesn't despawn for whatever reason.
				_ = Despawn.ServerSingle(gameObject);
			}

			// wirecutters can be used to cut this cable
			Vector3Int worldPosInt = interaction.WorldPositionTarget.RoundTo2Int().To3Int();
			var matrixInfo = MatrixManager.AtPoint(worldPosInt, true);
			var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixInfo);
			var matrix = matrixInfo?.Matrix;

			if (matrix == null || matrix.IsClearUnderfloorConstruction(localPosInt, true) == false) return;

			ToolUtils.ServerPlayToolSound(interaction);

			var bar = StandardProgressAction.Create(
				new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false), Remove);
			bar.ServerStartProgress(interaction.Performer.RegisterTile(), progressTimeToRemove, interaction.Performer);
		}
	}
}
