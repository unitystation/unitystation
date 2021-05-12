using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Systems.Electricity;

namespace Objects.Electrical
{
	/// <summary>
	/// Cable coil which can be applied to the ground to lay cable.
	/// </summary>
	[RequireComponent(typeof(Pickupable))]
	public class CableCoil : NetworkBehaviour, ICheckedInteractable<ConnectionApply>
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

		public bool WillInteract(ConnectionApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			//can only be used on tiles
			if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;
			// If there's a table, we should drop there
			if (MatrixManager.IsTableAtAnyMatrix(Vector3Int.RoundToInt(interaction.WorldPositionTarget), side == NetworkSide.Server))
			{
				return false;
			}
			return true;
		}

		public void ServerPerformInteraction(ConnectionApply interaction)
		{
			var cableCoil = interaction.HandObject.GetComponent<CableCoil>();
			if (cableCoil != null)
			{
				Vector3Int worldPosInt = Vector3Int.RoundToInt(interaction.WorldPositionTarget);
				var matrixInfo = MatrixManager.AtPoint(worldPosInt, true);
				var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixInfo);
				var matrix = matrixInfo?.Matrix;

				// if there is no matrix or IsClearUnderfloor == false - return
				if (matrix == null || matrix.IsClearUnderfloorConstruction(localPosInt, true) == false)
				{
					return;
				}

				Connection WireEndB = interaction.WireEndB;
				Connection WireEndA = interaction.WireEndA;

				if (WireEndB != Connection.NA)
				{
					// high voltage cables can't connect diagonally
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
						switch (WireEndA)
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
					// Builds the cable using the position and connections.
					BuildCable(localPosInt, interaction.Performer.transform.parent, WireEndA, WireEndB, interaction);
					// Consume a cable from the hand.
					// TODO: We sometimes need to consume more than one cable coil.
					//       Either we cut one joint at a time, or retrieve the number of
					//       cables.
					Inventory.ServerConsume(interaction.HandSlot, 1);
				}
			}
		}

		private void BuildCable(Vector3 position, Transform parent, Connection WireEndA, Connection WireEndB,
			ConnectionApply interaction)
		{
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			FindOverlapsAndCombine(position, WireEndA, WireEndB, interaction);
		}

		/// <summary>
		/// TODO: Document this.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="WireEndA"></param>
		/// <param name="WireEndB"></param>
		/// <param name="interaction"></param>
		public void FindOverlapsAndCombine(Vector3 position, Connection WireEndA, Connection WireEndB,
			ConnectionApply interaction)
		{
			// If the wire ends are overlapping...
			if (WireEndA == Connection.Overlap | WireEndB == Connection.Overlap)
			{
				// TODO: What is this...? Needs more documentation.
				bool isA;
				if (WireEndA == Connection.Overlap)
				{
					isA = true;
				}
				else
				{
					isA = false;
				}

				List<IntrinsicElectronicData> IEnumerableEconns =
					interaction.Performer.GetComponentInParent<Matrix>().GetElectricalConnections(position.RoundToInt());
				List<IntrinsicElectronicData> Econns = new List<IntrinsicElectronicData>(IEnumerableEconns);

				// TODO: What does this do? By this, we're removing all elements from
				//       the list, but then adding this empty, but with-capacity list
				//       to this.
				IEnumerableEconns.Clear();
				ElectricalPool.PooledFPCList.Add(IEnumerableEconns);

				// First, make sure we actually found electrical connections.
				if (Econns != null)
				{
					// Now, we loop through each electrical connection.
					for (int i = 0; i < Econns.Count; i++)
					{
						// If the power type of the current cable matches the the
						// connection's category...
						if (powerTypeCategory == Econns[i].Categorytype)
						{
							// If the end of the wire is overlapping...
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

								// Destroy the current electrical connection.
								Econns[i].DestroyThisPlease();
								// Get the electrical cable tile with the wire connection direction.
								ElectricalCableTile tile = ElectricityFunctions.RetrieveElectricalTile(WireEndA, WireEndB,
									powerTypeCategory);
								// Then, add an electrical node at the tile.
								interaction.Performer.GetComponentInParent<Matrix>()
									.AddElectricalNode(position.RoundToInt(), tile, true);

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
								ElectricalCableTile tile = ElectricityFunctions.RetrieveElectricalTile(WireEndA, WireEndB,
									powerTypeCategory);
								interaction.Performer.GetComponentInParent<Matrix>()
									.AddElectricalNode(position.RoundToInt(), tile, true);

								return;
							}
						}
					}
				}
			}

			var _tile = ElectricityFunctions.RetrieveElectricalTile(WireEndA, WireEndB,
				powerTypeCategory);
			interaction.Performer.GetComponentInParent<Matrix>().AddElectricalNode(position.RoundToInt(), _tile, true);
		}
	}
}
