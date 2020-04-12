using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Cable coil which can be applied to the ground to lay cable.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class CableCoil : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public WiringColor CableType;
	public GameObject CablePrefab;

	public Connection GetDirectionFromFaceDirection(GameObject originator) {
		var playerScript = originator.GetComponent<PlayerScript>();
		switch (playerScript.CurrentDirection.ToString())
		{
			case "Left":
				{
					return (Connection.West);
				}
			case "Right":
				{

					return (Connection.East);
				}
			case "Up":
				{

					return (Connection.North);
				}
			case "Down":
				{

					return (Connection.South);
				}
		}
		return (Connection.NA);

	}


	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only be used on tiles
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;

		// If there's a table, we should drop there
		if (MatrixManager.IsTableAt(interaction.WorldPositionTarget.RoundToInt(), side == NetworkSide.Server))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var cableCoil = interaction.HandObject.GetComponent<CableCoil>();
		if (cableCoil != null)
		{
			Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
			MatrixInfo matrix = MatrixManager.AtPoint(worldPosInt, true);
			var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);
			if (matrix.Matrix != null)
			{
				//can't place wires here
				if (!matrix.Matrix.IsClearUnderfloorConstruction(localPosInt, true))
				{
					return;
				}
			}
			else {
				//no matrix found to place wires in
				return;
			}

			var roundTargetWorldPosition = interaction.WorldPositionTarget.RoundToInt();
			Vector3 PlaceDirection = interaction.Performer.Player().Script.WorldPos - (Vector3)roundTargetWorldPosition;
			Connection WireEndB = Connection.NA;
			if (PlaceDirection == Vector3.up) { WireEndB = Connection.North; }
			else if (PlaceDirection == Vector3.down) { WireEndB = Connection.South; }
			else if (PlaceDirection == Vector3.right) { WireEndB = Connection.East; }
			else if (PlaceDirection == Vector3.left) { WireEndB = Connection.West; }

			else if (PlaceDirection == Vector3.down + Vector3.left) { WireEndB = Connection.SouthWest; }
			else if (PlaceDirection == Vector3.down + Vector3.right) { WireEndB = Connection.SouthEast; }
			else if (PlaceDirection == Vector3.up + Vector3.left) { WireEndB = Connection.NorthWest; }
			else if (PlaceDirection == Vector3.up + Vector3.right) { WireEndB = Connection.NorthEast; }
			else if (PlaceDirection == Vector3.zero) { WireEndB = GetDirectionFromFaceDirection(interaction.Performer); }

			if (WireEndB != Connection.NA)
			{
				if (CableType == WiringColor.high)
				{
					switch (WireEndB)
					{
						case Connection.NorthEast:
							return;
						case Connection.NorthWest:
							return;
						case Connection.SouthWest:
							return;
						case Connection.SouthEast:
							return;
					}

				}
				var econs = interaction.Performer.GetComponentInParent<Matrix>().GetElectricalConnections(localPosInt);
				foreach (var con in econs) {
					if (con.WireEndA == Connection.Overlap || con.WireEndB == Connection.Overlap) {
						if (con.WireEndA == WireEndB || con.WireEndB == WireEndB)
						{
							Chat.AddExamineMsgToClient("There is already a cable at that position");
							econs.Clear();
							ElectricalPool.PooledFPCList.Add(econs);
							return;
						}
						foreach (var Econ in econs)
						{
							if (Econ.WireEndA == WireEndB || Econ.WireEndB == WireEndB)
							{
								if (con.WireEndA == Econ.WireEndA || con.WireEndB == Econ.WireEndA){
									Chat.AddExamineMsgToClient("There is already a cable at that position");
									econs.Clear();
									ElectricalPool.PooledFPCList.Add(econs);
									return;
								}
								else if (con.WireEndA == Econ.WireEndB || con.WireEndB == Econ.WireEndB){
									Chat.AddExamineMsgToClient("There is already a cable at that position");
									econs.Clear();
									ElectricalPool.PooledFPCList.Add(econs);
									return;
								}
							}
						}
					}
				}
				econs.Clear();
				ElectricalPool.PooledFPCList.Add(econs);
				BuildCable(roundTargetWorldPosition, interaction.Performer.transform.parent, WireEndB);
				Inventory.ServerConsume(interaction.HandSlot, 1);
			}
		}
	}

	private void BuildCable(Vector3 position, Transform parent, Connection WireEndB)
	{
		Connection WireEndA = Connection.Overlap;
		GameObject Cable = Spawn.ServerPrefab(CablePrefab, position, parent).GameObject;
		ElectricalManager.Instance.electricalSync.StructureChange = true;
		//ElectricalCableMessage.Send(Cable, WireEndA, WireEndB, CableType);
		var CableInheritance = Cable.GetComponent<CableInheritance>();
		CableInheritance.IsInGamePlaced = true;
		CableInheritance.SetDirection(WireEndB, WireEndA, CableType);

	}
}
