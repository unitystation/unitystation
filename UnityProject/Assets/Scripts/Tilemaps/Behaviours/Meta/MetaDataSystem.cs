using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

/// <summary>
/// Subsystem behavior which manages updating the MetaDataNodes and simulation that affects them for a given matrix.
/// </summary>
public class MetaDataSystem : SubsystemBehaviour
{
	// Set higher priority to ensure that it is executed before other systems
	public override int Priority => 100;

	/// <summary>
	/// Nodes which exist in space next to room tiles of the matrix.
	/// These are regularly checked for tiles from other matrices, i.e. they build the bridge to other matrices
	/// </summary>
	private ConcurrentDictionary<MetaDataNode, MetaDataNode> externalNodes;

	/// <summary>
	/// Matrix this system is managing the MetaDataNodes for.
	/// </summary>
	private Matrix matrix;

	private int roomCounter = 0;

	public override void Awake()
	{
		base.Awake();

		matrix = GetComponentInChildren<Matrix>(true);
		externalNodes = new ConcurrentDictionary<MetaDataNode, MetaDataNode>();
	}

	void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public override void Initialize()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		if (MatrixManager.IsInitialized)
		{
			Stopwatch Dsw = new Stopwatch();
			Dsw.Start();
			matrix.UnderFloorLayer.InitialiseUnderFloorUtilities();
			Dsw.Stop();
			Logger.Log("Initialise Station Utilities (Power cables, Atmos pipes): " + Dsw.ElapsedMilliseconds + " ms", Category.Matrix);
		}

		sw.Stop();

		Logger.Log("MetaData init: " + sw.ElapsedMilliseconds + " ms", Category.Matrix);
	}

	public override void UpdateAt(Vector3Int localPosition)
	{
		MetaDataNode node = metaDataLayer.Get(localPosition);

		MetaUtils.RemoveFromNeighbors(node);
		externalNodes.TryRemove(node, out MetaDataNode nothing);

		node.IsClosedAirlock = false;

		// If the node is atmos passable (i.e. space or room), we need to setup its neighbors again, otherwise it's occupied and does need a neighbor check
		if (metaTileMap.IsAtmosPassableAt(localPosition, true))
		{
			node.ClearNeighbors();

			node.Type = metaTileMap.IsSpaceAt(localPosition, true) ? NodeType.Space : NodeType.Room;
			SetupNeighbors(node);
			MetaUtils.AddToNeighbors(node);
		}
		else
		{
			node.Type = NodeType.Occupied;
			if (matrix.GetFirst<RegisterDoor>(localPosition, true))
			{
				node.IsClosedAirlock = true;
			}
		}
	}

	private void SetupNeighbors(MetaDataNode node)
	{
		Vector3 nodeWorldPosition = MatrixManager.LocalToWorldInt(node.Position, MatrixManager.Get(matrix.Id));

		// Look in every direction for neighboring tiles.
		foreach (Vector3Int dir in MetaUtils.Directions)
		{
			Vector3Int neighbor = dir + node.Position;

			if (metaTileMap.IsSpaceAt(neighbor, true))
			{
				// if current node is a room, but the neighboring is a space tile, this node needs to be checked regularly for changes by other matrices
				if (node.IsRoom && !externalNodes.ContainsKey(node))
				{
					externalNodes[node] = node;
				}

				// If the node is not space, check other matrices if it has a tile next to this node.
				if (!node.IsSpace)
				{
					Vector3 neighborWorldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

					// if matrixManager says, it's not space at the neighboring position, there must be a matrix with a non-space tile
					if (!MatrixManager.IsSpaceAt(neighborWorldPosition.RoundToInt(), true))
					{
						MatrixInfo matrixInfo = MatrixManager.AtPoint(neighborWorldPosition.RoundToInt(), true);

						// ignore tilemap of current node
						if (matrixInfo.MetaTileMap != metaTileMap)
						{
							// Check if atmos can pass to the neighboring position
							Vector3Int neighborlocalPosition = MatrixManager.WorldToLocalInt(neighborWorldPosition, matrixInfo);
							Vector3Int nodeLocalPosition = MatrixManager.WorldToLocalInt(nodeWorldPosition, matrixInfo);

							if (matrixInfo.MetaTileMap.IsAtmosPassableAt(nodeLocalPosition, neighborlocalPosition, true))
							{
								// add node of other matrix to the neighbors of the current node
								node.AddNeighbor(matrixInfo.MetaDataLayer.Get(neighborlocalPosition), dir);
							}

							// skip other checks for neighboring tile on local tilemap, to prevent the space tile to be added as a neighbor
							continue;
						}
					}
				}
			}

			// If neighboring tile on local tilemap is atmos passable, add it as a neighbor
			if (metaTileMap.IsAtmosPassableAt(node.Position, neighbor, true))
			{
				MetaDataNode neighborNode = metaDataLayer.Get(neighbor);

				if (metaTileMap.IsSpaceAt(neighbor, true))
				{
					neighborNode.Type = NodeType.Space;
				}

				node.AddNeighbor(neighborNode, dir);
			}
		}
	}

	void UpdateMe()
	{
		if (CustomNetworkManager.Instance._isServer == false) return;

		foreach (MetaDataNode node in externalNodes.Keys)
		{
			subsystemManager.UpdateAt(node.Position);
		}
	}
}