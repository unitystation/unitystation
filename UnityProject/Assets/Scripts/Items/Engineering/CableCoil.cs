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

		/// <summary>
		/// Message that is send to the client when attempting to build a cable
		/// that already exists at a given position.
		/// </summary>
		private const string MSG_BUILD_CONFLICT = "There is already a cable at that position.";

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
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			//can only be used on tiles
			if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;
			// If there's a table, we should drop there
			if (MatrixManager.IsTableAt(Vector3Int.RoundToInt(interaction.WorldPositionTarget), side == NetworkSide.Server))
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
				if (IsLegalConnection(wireEndA, wireEndB))
				{
					// Grab a list of electrical connections from the matrix at
					// the given location.
					var eConnList =
						interaction.Performer.GetComponentInParent<Matrix>().GetElectricalConnections(localPosInt);

					// Find any cables on the matrix that conflicts with our
					// proposed connections.
					// TODO: What's the point of the nested for loop?
					foreach (IntrinsicElectronicData eConnI in eConnList.List)
					{
						if (eConnI.WireEndA == Connection.Overlap || eConnI.WireEndB == Connection.Overlap)
						{
							if (eConnI.WireEndA == wireEndB || eConnI.WireEndB == wireEndB)
							{
								MsgAndAddToPool( eConnList, MSG_BUILD_CONFLICT);
								return;
							}

							foreach (IntrinsicElectronicData eConnJ in eConnList.List)
							{
								if (eConnJ.WireEndA == wireEndB || eConnJ.WireEndB == wireEndB)
								{
									if (eConnI.WireEndA == eConnJ.WireEndA || eConnI.WireEndB == eConnJ.WireEndA)
									{
										MsgAndAddToPool( eConnList, MSG_BUILD_CONFLICT);
										return;
									}
									else if (eConnI.WireEndA == eConnJ.WireEndB || eConnI.WireEndB == eConnJ.WireEndB)
									{
										MsgAndAddToPool( eConnList, MSG_BUILD_CONFLICT);
										return;
									}
								}
							}
						}
					}


					MsgAndAddToPool( eConnList, null);
					BuildCable(localPosInt, wireEndA, wireEndB, interaction);
				}
			}
		}

		/// <summary>
		/// Adds examine message to the client (if provided), clears the
		/// electrical connections list, and adds it to the electrical pool.
		/// </summary>
		/// <param name="eConnList">List of electrical connections. Will not add to electrical pool if null.</param>
		/// <param name="addMsg">Message to show as an examine message. Will not show anything if null.</param>

		private void MsgAndAddToPool( ElectricalPool.IntrinsicElectronicDataList eConnList, string msg)
		{
			if (msg != null)
			{
				Chat.AddExamineMsgToClient(msg);
			}

			eConnList?.Pool();
		}

		/// <summary>
		/// Checks if the connection ends are legal. In this case, high-voltage
		/// cables cannot connect diagonally and no connections are illegal. All
		/// other connections are legal.
		/// </summary>
		/// <param name="wireEndA">The first connection we're building from.</param>
		/// <param name="wireEndB">The second connection we're building to.</param>
		/// <returns>false if attempting to connect high voltage cables
		/// diagonally or no connection, true otherwise.</returns>
		private bool IsLegalConnection(Connection wireEndA, Connection wireEndB)
		{
			if (wireEndA == Connection.NA && wireEndB == Connection.NA)
			{
				return false;
			}

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
		/// <param name="position">Position of the cell we're building the cable at.</param>
		/// <param name="wireEndA">The first connection we're building from.</param>
		/// <param name="wireEndB">The second connection we're building to.</param>
		/// <param name="interaction">Object to allow us to interact with the server.</param>
		public void FindOverlapsAndCombine(Vector3 position, Connection wireEndA, Connection wireEndB,
			ConnectionApply interaction)
		{
			// If the wire ends are overlapping...
			if (wireEndA == Connection.Overlap || wireEndB == Connection.Overlap)
			{
				// Stores boolean representing whether or not wireEndA is an
				// overlap.
				bool isWireEndAOverlap = wireEndA == Connection.Overlap;
				var IEnumerableEconns = interaction.Performer.GetComponentInParent<Matrix>()
					.GetElectricalConnections(position.RoundToInt());
				List<IntrinsicElectronicData> eConns = new List<IntrinsicElectronicData>(IEnumerableEconns.List);

				// TODO: What does this do? By this, we're removing all
				//       elements from the list, but then adding this empty,
				//       but with-capacity list to this. Document this.

				IEnumerableEconns.Pool();

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
		private void ReplaceEConn(Vector3 position, IntrinsicElectronicData eConn, Connection wireEndA,
			Connection wireEndB, ConnectionApply interaction)
		{
			// Cost of cable coils to construct the original cable tile. Assume
			// 0 until we verify whether or not we are given an electrical
			// connection.
			int oldTileCost = 0;

			// Get the cost of the old tile. Then destroy the current
			// electrical connection only if we were given a connection.
			if (eConn != null)
			{
				oldTileCost = eConn.MetaDataPresent.RelatedTile.SpawnAmountOnDeconstruct;
				eConn.DestroyThisPlease();
			}

			// Get the electrical cable tile with the wire connection direction.
			ElectricalCableTile tile =
				ElectricityFunctions.RetrieveElectricalTile(wireEndA, wireEndB, powerTypeCategory);

			// We only want to consume the difference needed to build the new
			// cable.
			int newTileCost = tile.SpawnAmountOnDeconstruct;
			int finalCost = newTileCost - oldTileCost;

			// Attempt to consume the cables in the hands using the final cost
			// we found.
			if (Inventory.ServerConsume(interaction.HandSlot, finalCost))
			{
				// Then, add an electrical node at the tile.
				interaction.Performer.GetComponentInParent<Matrix>().AddElectricalNode(position.RoundToInt(), tile, true);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.PerformerPlayerScript.PlayerInfo,
					$"You don't have enough cable to place");
			}
		}
	}
}
