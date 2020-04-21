﻿using System.Collections;
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
	public PowerTypeCategory powerTypeCategory;

	public Connection GetDirectionFromFaceDirection(GameObject originator)
	{
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
			else
			{
				//no matrix found to place wires in
				return;
			}

			var roundTargetWorldPosition = interaction.WorldPositionTarget.RoundToInt();
			Vector3 PlaceDirection =
				interaction.Performer.Player().Script.WorldPos - (Vector3) roundTargetWorldPosition;
			Connection WireEndB = Connection.NA;
			if (PlaceDirection == Vector3.up)
			{
				WireEndB = Connection.North;
			}
			else if (PlaceDirection == Vector3.down)
			{
				WireEndB = Connection.South;
			}
			else if (PlaceDirection == Vector3.right)
			{
				WireEndB = Connection.East;
			}
			else if (PlaceDirection == Vector3.left)
			{
				WireEndB = Connection.West;
			}

			else if (PlaceDirection == Vector3.down + Vector3.left)
			{
				WireEndB = Connection.SouthWest;
			}
			else if (PlaceDirection == Vector3.down + Vector3.right)
			{
				WireEndB = Connection.SouthEast;
			}
			else if (PlaceDirection == Vector3.up + Vector3.left)
			{
				WireEndB = Connection.NorthWest;
			}
			else if (PlaceDirection == Vector3.up + Vector3.right)
			{
				WireEndB = Connection.NorthEast;
			}
			else if (PlaceDirection == Vector3.zero)
			{
				WireEndB = GetDirectionFromFaceDirection(interaction.Performer);
			}

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
				foreach (var con in econs)
				{
					if (con.WireEndA == Connection.Overlap || con.WireEndB == Connection.Overlap)
					{
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
								if (con.WireEndA == Econ.WireEndA || con.WireEndB == Econ.WireEndA)
								{
									Chat.AddExamineMsgToClient("There is already a cable at that position");
									econs.Clear();
									ElectricalPool.PooledFPCList.Add(econs);
									return;
								}
								else if (con.WireEndA == Econ.WireEndB || con.WireEndB == Econ.WireEndB)
								{
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
				BuildCable(localPosInt, interaction.Performer.transform.parent, WireEndB, interaction);
				Inventory.ServerConsume(interaction.HandSlot, 1);
			}
		}
	}

	private void BuildCable(Vector3 position, Transform parent, Connection WireEndB, PositionalHandApply interaction)
	{
		Connection WireEndA = Connection.Overlap;
		ElectricalManager.Instance.electricalSync.StructureChange = true;
		FindOverlapsAndCombine(position, WireEndA, WireEndB, interaction);

	}

	public void FindOverlapsAndCombine(Vector3 position, Connection WireEndA, Connection WireEndB,
		PositionalHandApply interaction)
	{
		if (WireEndA == Connection.Overlap | WireEndB == Connection.Overlap)
		{
			bool isA;
			if (WireEndA == Connection.Overlap)
			{
				isA = true;
			}
			else
			{
				isA = false;
			}

			List<IntrinsicElectronicData> Econns = new List<IntrinsicElectronicData>();
			var IEnumerableEconns = interaction.Performer.GetComponentInParent<Matrix>()
				.GetElectricalConnections(position.RoundToInt());
			foreach (var T in IEnumerableEconns)
			{
				Econns.Add(T);
			}

			IEnumerableEconns.Clear();
			ElectricalPool.PooledFPCList.Add(IEnumerableEconns);
			int i = 0;
			if (Econns != null)
			{
				while (!(i >= Econns.Count))
				{
					if (powerTypeCategory == Econns[i].Categorytype)
					{
						if (Econns[i].WireEndA == Connection.Overlap)
						{
							if (isA)
							{
								WireEndA = Econns[i].WireEndB;
							}
							else
							{
								WireEndB = Econns[i].WireEndB;
							}
							Econns[i].DestroyThisPlease();
							var tile = ElectricityFunctions.RetrieveElectricalTile(WireEndA, WireEndB,
								powerTypeCategory);
							interaction.Performer.GetComponentInParent<Matrix>().AddElectricalNode(position.RoundToInt(),tile,true);

							return;
						}
						else if (Econns[i].WireEndB == Connection.Overlap)
						{
							if (isA)
							{
								WireEndA = Econns[i].WireEndA;
							}
							else
							{
								WireEndB = Econns[i].WireEndA;
							}
							Econns[i].DestroyThisPlease();
							var tile = ElectricityFunctions.RetrieveElectricalTile(WireEndA, WireEndB,
								powerTypeCategory);
							interaction.Performer.GetComponentInParent<Matrix>().AddElectricalNode(position.RoundToInt(),tile,true);

							return;
						}
					}

					i++;
				}
			}
		}
		var _tile = ElectricityFunctions.RetrieveElectricalTile(WireEndA, WireEndB,
			powerTypeCategory);
		interaction.Performer.GetComponentInParent<Matrix>().AddElectricalNode(position.RoundToInt(),_tile,true);

	}
}