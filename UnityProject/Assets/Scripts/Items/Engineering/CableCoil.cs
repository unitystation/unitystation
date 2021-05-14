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

		/// <summary>
		/// <para>Facilitates the interaction of the cable coil in hand.</para>
		/// First checks if the connection we're trying to make is legal, then
		/// checks against all electrical connections on the cell in the matrix
		/// to see if the connection already exists. If everything is good,
		/// this proceeds to add the new cable to the electrical pool and
		/// builds the cable.
		/// </summary>
		/// <param name="interaction"></param>
		public void ServerPerformInteraction(ConnectionApply interaction)
		{
			CableCoil cableCoil = interaction.HandObject.GetComponent<CableCoil>();

			if (cableCoil != null)
			{
				Vector3Int worldPosInt = Vector3Int.RoundToInt(interaction.WorldPositionTarget);
				MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPosInt, true);
				Vector3Int localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixInfo);
				Matrix matrix = matrixInfo?.Matrix;

				// if there is no matrix or IsClearUnderfloor == false - return
				if (matrix == null || matrix.IsClearUnderfloorConstruction(localPosInt, true) == false)
				{
					return;
				}

				// Get the starting and ending connections.
				Connection wireEndA = interaction.WireEndA;
				Connection wireEndB = interaction.WireEndB;

				// Make sure that the ending connection actually exists.
				if (wireEndB != Connection.NA)
				{
					// Check if the connection is legal.
					if (!IsLegalConnection(wireEndA, wireEndB))
					{
						return;
					}

					List<IntrinsicElectronicData> eConnList =
						interaction.Performer.GetComponentInParent<Matrix>().GetElectricalConnections(localPosInt);

					// TODO: There is a LOT of repeat code here.
					// TODO: What's the point of the nested for loop?
					foreach (IntrinsicElectronicData eConnI in eConnList)
					{
						if (eConnI.WireEndA == Connection.Overlap || eConnI.WireEndB == Connection.Overlap)
						{
							if (eConnI.WireEndA == wireEndB || eConnI.WireEndB == wireEndB)
							{
								Chat.AddExamineMsgToClient("There is already a cable at that position");
								eConnList.Clear();
								ElectricalPool.PooledFPCList.Add(eConnList);
								return;
							}

							foreach (IntrinsicElectronicData eConnJ in eConnList)
							{
								if (eConnJ.WireEndA == wireEndB || eConnJ.WireEndB == wireEndB)
								{
									if (eConnI.WireEndA == eConnJ.WireEndA || eConnI.WireEndB == eConnJ.WireEndA)
									{
										Chat.AddExamineMsgToClient("There is already a cable at that position");
										eConnList.Clear();
										ElectricalPool.PooledFPCList.Add(eConnList);
										return;
									}
									else if (eConnI.WireEndA == eConnJ.WireEndB || eConnI.WireEndB == eConnJ.WireEndB)
									{
										Chat.AddExamineMsgToClient("There is already a cable at that position");
										eConnList.Clear();
										ElectricalPool.PooledFPCList.Add(eConnList);
										return;
									}
								}
							}
						}
					}

					eConnList.Clear();
					ElectricalPool.PooledFPCList.Add(eConnList);
					// Builds the cable using the position and connections.
					BuildCable(localPosInt, wireEndA, wireEndB, interaction);
				}
			}
		}

		/// <summary>
		/// Checks if the connection ends are legal. In this case, high-voltage
		/// cables cannot cannot diagonally. All other connections are legal.
		/// </summary>
		/// <param name="wireEndA">The first connection we're building from.</param>
		/// <param name="wireEndB">The second connection we're building to.</param>
		/// <returns>false if attempting to connect high voltage cables
		/// diagonally, true otherwise.</returns>
		private bool IsLegalConnection(Connection wireEndA, Connection wireEndB)
		{
			if (CableType == WiringColor.high)
			{
				switch (wireEndA)
				{
					case Connection.NorthEast:
					case Connection.NorthWest:
					case Connection.SouthWest:
					case Connection.SouthEast:
						return false;
				}
				switch (wireEndB)
				{
					case Connection.NorthEast:
					case Connection.NorthWest:
					case Connection.SouthWest:
					case Connection.SouthEast:
						return false;
				}
			}

			return true;
		}

		private void BuildCable(Vector3 position, Connection wireEndA, Connection wireEndB,
			ConnectionApply interaction)
		{
			ElectricalManager.Instance.electricalSync.StructureChange = true;
			FindOverlapsAndCombine(position, wireEndA, wireEndB, interaction);
		}

		/// <summary>
		/// Finds overlapping wires and combines them to create a new
		/// connection.
		/// </summary>
		/// <param name="position">Position of the cell we're building thecable at.</param>
		/// <param name="wireEndA">The first connection we're building from.</param>
		/// <param name="wireEndB">The second connection we're building to.</param>
		/// <param name="interaction">Object to allow us to interact with the server.</param>
		public void FindOverlapsAndCombine(Vector3 position, Connection wireEndA, Connection wireEndB,
			ConnectionApply interaction)
		{
			// If the wire ends are overlapping...
			if (wireEndA == Connection.Overlap | wireEndB == Connection.Overlap)
			{
				// Stores boolean representing whether or not wireEndA is an
				// overlap.
				bool isWireEndAOverlap = wireEndA == Connection.Overlap;
				List<IntrinsicElectronicData> IEnumerableEconns = interaction.Performer.GetComponentInParent<Matrix>()
					.GetElectricalConnections(position.RoundToInt());
				List<IntrinsicElectronicData> eConns = new List<IntrinsicElectronicData>(IEnumerableEconns);

				// TODO: What does this do? By this, we're removing all
				//       elements from the list, but then adding this empty,
				//       but with-capacity list to this. Document this.
				IEnumerableEconns.Clear();
				ElectricalPool.PooledFPCList.Add(IEnumerableEconns);

				// First, make sure we actually found electrical connections.
				if (eConns != null)
				{
					// Now, we loop through each electrical connection.
					for (int i = 0; i < eConns.Count; i++)
					{
						// We only want to combine the cables if the type of
						// cables we're looking at is the same category type
						// (i.e. low, medium, or high).
						if (powerTypeCategory == eConns[i].Categorytype)
						{
							// If the first connection we found is an overlap
							// and our starting connection is an overlap, then
							// we want to set the first end as the overlapping
							// cable's second end. Else, we set our ending
							// connection as the overlapping cable's second
							// end.
							if (eConns[i].WireEndA == Connection.Overlap)
							{
								if (isWireEndAOverlap)
								{
									wireEndA = eConns[i].WireEndB;
								}
								else
								{
									wireEndB = eConns[i].WireEndB;
								}

								ReplaceEConn(position, eConns[i], wireEndA, wireEndB, interaction);
								return;
							}
							// Else, if the second connection we found is an
							// overlap and our starting connection is an
							// overlap, then we want to set the first end as
							// the overlapping cable's first end. Else, we set
							// our ending connection as the overlapping cable's
							// first end.
							else if (eConns[i].WireEndB == Connection.Overlap)
							{
								if (isWireEndAOverlap)
								{
									wireEndA = eConns[i].WireEndA;
								}
								else
								{
									wireEndB = eConns[i].WireEndA;
								}

								ReplaceEConn(position, eConns[i], wireEndA, wireEndB, interaction);
								return;
							}
						}
					}
				}
			}

			ReplaceEConn(position, null, wireEndA, wireEndB, interaction);
		}

		/// <summary>
		/// <para>Replaces, builds a new electrical connection, and consumes
		/// cables from the hand.</para>
		/// <para>Destroys the electrical connection (if we're given one) and
		/// then adds a new electrical connection by adding an electrical
		/// node. We then calculate and consume a number of cables from the
		/// hand.</para>
		/// </summary>
		/// <param name="position">Position of the electrical connection.</param>
		/// <param name="eConn">Electrical connection we're replacing.</param>
		/// <param name="wireEndA">Connection direction of one end.</param>
		/// <param name="wireEndB">Connection direction of the other end.</param>
		/// <param name="interaction"></param>
		private void ReplaceEConn(Vector3 position, IntrinsicElectronicData eConn, Connection wireEndA,
			Connection wireEndB, ConnectionApply interaction)
		{
			// Cost of cable coils to construct the original cable tile. Assume
			// 0 until we verify whether or not we are given an electrical
			// connection.
			int oldTileCost = 0;

			// Get the cost of the old tile. Thend estroy the current
			// electrical connection only if we were given a connection.
			if (eConn != null)
			{
				oldTileCost = eConn.MetaDataPresent.RelatedTile.SpawnAmountOnDeconstruct;
				eConn.DestroyThisPlease();
			}

			// Get the electrical cable tile with the wire connection direction.
			ElectricalCableTile tile =
				ElectricityFunctions.RetrieveElectricalTile(wireEndA, wireEndB, powerTypeCategory);
			// Then, add an electrical node at the tile.
			interaction.Performer.GetComponentInParent<Matrix>().AddElectricalNode(position.RoundToInt(), tile, true);

			// We only want to consume the difference needed to build the new
			// cable.
			int newTileCost = tile.SpawnAmountOnDeconstruct;
			int finalCost = newTileCost - oldTileCost;

			// Finally, consume the cables in the hands using the final cost
			// we found.
			Inventory.ServerConsume(interaction.HandSlot, finalCost);
		}
	}
}
