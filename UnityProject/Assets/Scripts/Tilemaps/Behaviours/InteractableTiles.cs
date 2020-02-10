using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Main entry point for Tile Interaction system.
/// Component which allows all the tiles of a matrix to be interacted with using their configured
/// TileInteractions.
///
/// Also provides various utility methods for working with tiles.
/// </summary>
public class InteractableTiles : NetworkBehaviour, IClientInteractable<PositionalHandApply>
{
	private MetaTileMap metaTileMap;
	private Matrix matrix;
	private TileChangeManager tileChangeManager;

	/// <summary>
	/// MetaTileMap that can be interacted with
	/// </summary>
	public MetaTileMap MetaTileMap => metaTileMap;

	/// <summary>
	/// Matrix that can be interacted with
	/// </summary>
	public Matrix Matrix => matrix;

	/// <summary>
	/// Tile change manager that can be interacted with
	/// </summary>
	public TileChangeManager TileChangeManager => tileChangeManager;

	private Layer floorLayer;
	public Layer FloorLayer => floorLayer;
	private Layer baseLayer;
	public Layer BaseLayer => baseLayer;
	private Layer wallLayer;
	public Layer WallLayer => wallLayer;
	private Layer windowLayer;
	public Layer WindowLayer => windowLayer;
	private ObjectLayer objectLayer;
	public ObjectLayer ObjectLayer => objectLayer;

	private Layer grillTileMap;

	private void Start()
	{
		metaTileMap = GetComponentInChildren<MetaTileMap>();
		matrix = GetComponentInChildren<Matrix>();
		objectLayer = GetComponentInChildren<ObjectLayer>();
		tileChangeManager = GetComponent<TileChangeManager>();
		CacheTileMaps();
	}

	/// <summary>
	/// Gets the interactable tiles for the matrix at the indicated world position. Never returns null - always
	/// returns the interactable tiles for the appropriate matrix.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public static InteractableTiles GetAt(Vector2 worldPos, bool isServer)
	{
		var matrixInfo = MatrixManager.AtPoint(worldPos.RoundToInt(), isServer);
		var matrix = matrixInfo.Matrix;
		var tileChangeManager = matrix.GetComponentInParent<TileChangeManager>();
		return tileChangeManager.GetComponent<InteractableTiles>();
	}

	/// <summary>
	/// Gets the interactable tiles for the matrix at the indicated world position. Never returns null - always
	/// returns the interactable tiles for the appropriate matrix.
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public static InteractableTiles GetAt(Vector2 worldPos, NetworkSide side)
	{
		return GetAt(worldPos, side == NetworkSide.Server);
	}

	/// <summary>
	/// Gets the LayerTile of the tile at the indicated position, null if no tile there (open space).
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public LayerTile LayerTileAt(Vector2 worldPos, bool ignoreEffectsLayer = false)
	{
		Vector3Int pos = objectLayer.transform.InverseTransformPoint(worldPos).RoundToInt();

		return metaTileMap.GetTile(pos, ignoreEffectsLayer);
	}


	/// <summary>
	/// Converts the world position to a cell position on these tiles.
	/// </summary>
	/// <param name="worldPosition"></param>
	/// <returns></returns>
	public Vector3Int WorldToCell(Vector2 worldPosition)
	{
		return baseLayer.WorldToCell(worldPosition);
	}

	void CacheTileMaps()
	{
		var tilemaps = GetComponentsInChildren<Layer>(true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains("Floors"))
			{
				floorLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Base"))
			{
				baseLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Walls"))
			{
				wallLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Windows"))
			{
				windowLayer = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Grills"))
			{
				grillTileMap = tilemaps[i];
			}
		}
	}


	public bool Interact(PositionalHandApply interaction)
	{
		//translate to the tile interaction system

		//pass the interaction down to the basic tile
		LayerTile tile = LayerTileAt(interaction.WorldPositionTarget);
		if (tile is BasicTile basicTile)
		{
			var tileApply = new TileApply(interaction.Performer, interaction.UsedObject, interaction.Intent,
				(Vector2Int) WorldToCell(interaction.WorldPositionTarget), this, basicTile, interaction.HandSlot,
				interaction.TargetVector);

			var i = 0;
			foreach (var tileInteraction in basicTile.TileInteractions)
			{
				if (tileInteraction == null) continue;
				if (tileInteraction.WillInteract(tileApply, NetworkSide.Client) &&
				    Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction))
				{
					//request the tile interaction with this index
					RequestInteractMessage.SendTileApply(tileApply, this, tileInteraction, i);
					return true;
				}

				i++;
			}
		}

		return false;
	}


	//for internal IF2 usages only, does server side logic for processing tileapply
	public void ServerProcessInteraction(int tileInteractionIndex, GameObject performer, Vector2 targetVector,  GameObject processorObj, ItemSlot usedSlot, GameObject usedObject, Intent intent)
	{
		//find the indicated tile interaction
		var success = false;
		var worldPosTarget = (Vector2)performer.transform.position + targetVector;
		//pass the interaction down to the basic tile
		LayerTile tile = LayerTileAt(worldPosTarget);
		if (tile is BasicTile basicTile)
		{
			if (tileInteractionIndex >= basicTile.TileInteractions.Count)
			{
				//this sometimes happens due to lag, so bumping it down to trace
				Logger.LogTraceFormat("Requested TileInteraction at index {0} does not exist on this tile {1}.",
					Category.Interaction, tileInteractionIndex, basicTile.name);
				return;
			}

			var tileInteraction = basicTile.TileInteractions[tileInteractionIndex];
			var tileApply = new TileApply(performer, usedObject, intent,
				(Vector2Int) WorldToCell(worldPosTarget), this, basicTile, usedSlot,
				targetVector);

			if (tileInteraction.WillInteract(tileApply, NetworkSide.Server) &&
			    Cooldowns.TryStartServer(tileApply, CommonCooldowns.Instance.Interaction))
			{
				//perform
				tileInteraction.ServerPerformInteraction(tileApply);
			}
			else
			{
				tileInteraction.ServerRollbackClient(tileApply);
			}
		}
	}
}