using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;
using YamlDotNet.Samples;

/// <summary>
/// Main entry point for Tile Interaction system.
/// Component which allows all the tiles of a matrix to be interacted with using their configured
/// TileInteractions.
///
/// Also provides various utility methods for working with tiles.
/// </summary>
public class InteractableTiles : NetworkBehaviour, IClientInteractable<PositionalHandApply>, IExaminable, IClientInteractable<MouseDrop>
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
	private Layer underFloorLayer;
	public Layer UnderFloorLayer => underFloorLayer;

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
	/// Gets the LayerTile of the tile at the indicated position, null if no tile there (open space).
	/// </summary>
	/// <param name="worldPos"></param>
	/// <returns></returns>
	public LayerTile LayerTileAt(Vector2 worldPos, LayerTypeSelection ExcludedLayers)
	{
		Vector3Int pos = objectLayer.transform.InverseTransformPoint(worldPos).RoundToInt();

		return metaTileMap.GetTile(pos, ExcludedLayers);
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

			if (tilemaps[i].name.Contains("UnderFloor"))
			{
				underFloorLayer = tilemaps[i];
			}
		}
	}

	public string Examine(Vector3 pos)
	{
		// Get Tile at position
		LayerTile tile = LayerTileAt(pos);

		if (tile == null)
		{
			return "Space";
		}
		string msg = "This is a " + tile.DisplayName + ".";
		if (tile is IExaminable) msg += "\n" + (tile as IExaminable).Examine();

		return msg;
	}

	public bool Interact(PositionalHandApply interaction)
	{
		//translate to the tile interaction system

		Vector3Int localPosition = WorldToCell(interaction.WorldPositionTarget);
		//pass the interaction down to the basic tile
		LayerTile tile = LayerTileAt(interaction.WorldPositionTarget, true);
		if (tile is BasicTile basicTile)
		{
			// The underfloor layer can be composed of multiple tiles, iterate over them until interaction is found.
			if (basicTile.LayerType == LayerType.Underfloor)
			{
				foreach (var underFloorTile in matrix.UnderFloorLayer.GetAllTilesByType<BasicTile>(localPosition))
				{
					var underFloorApply = new TileApply(interaction.Performer, interaction.UsedObject, interaction.Intent,
						(Vector2Int) localPosition, this, underFloorTile, interaction.HandSlot, interaction.TargetVector);

					if (TryInteractWithTile(underFloorApply)) return true;
				}
			}
			else
			{
				var tileApply = new TileApply(interaction.Performer, interaction.UsedObject, interaction.Intent,
				(Vector2Int) localPosition, this, basicTile, interaction.HandSlot, interaction.TargetVector);

				return TryInteractWithTile(tileApply);
			}
		}
		return false;
	}

	bool TryInteractWithTile(TileApply interaction)
	{
		// Iterate over the interactions for the given tile until a valid one is found.
		foreach (var tileInteraction in interaction.BasicTile.TileInteractions)
		{
			if (tileInteraction == null) continue;
			if (tileInteraction.WillInteract(interaction, NetworkSide.Client) &&
				Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction))
			{
				//request the tile interaction with this index
				RequestInteractMessage.SendTileApply(interaction, this, tileInteraction);
				return true;
			}
		}

		return false;
	}

	//for internal IF2 usages only, does server side logic for processing tileapply
	public void ServerProcessInteraction(GameObject performer, Vector2 targetVector,  GameObject processorObj,
			ItemSlot usedSlot, GameObject usedObject, Intent intent, TileApply.ApplyType applyType)
	{
		//find the indicated tile interaction
		var worldPosTarget = (Vector2)performer.transform.position + targetVector;
		Vector3Int localPosition = WorldToCell(worldPosTarget);
		//pass the interaction down to the basic tile
		LayerTile tile = LayerTileAt(worldPosTarget, true);
		if (tile is BasicTile basicTile)
		{
			// check which tile interaction occurs in the correct order
			Logger.LogTraceFormat(
					"Server checking which tile interaction to trigger for TileApply on tile {0} at worldPos {1}",
					Category.Interaction, tile.name, worldPosTarget);

			if (basicTile.LayerType == LayerType.Underfloor)
			{
				foreach (var underFloorTile in matrix.UnderFloorLayer.GetAllTilesByType<BasicTile>(localPosition))
				{
					var underFloorApply = new TileApply(
							performer, usedObject, intent, (Vector2Int) localPosition,
							this, underFloorTile, usedSlot, targetVector, applyType);

					foreach (var tileInteraction in underFloorTile.TileInteractions)
					{
						if (tileInteraction == null) continue;
						if (tileInteraction.WillInteract(underFloorApply, NetworkSide.Server))
						{
							PerformTileInteract(underFloorApply);
							break;
						}
					}					
				}
			}
			else
			{
				var tileApply = new TileApply(
						performer, usedObject, intent, (Vector2Int) localPosition,
						this, basicTile, usedSlot, targetVector, applyType);

				PerformTileInteract(tileApply);
			}
		}
	}

	private void PerformTileInteract(TileApply interaction)
	{
		foreach (var tileInteraction in interaction.BasicTile.TileInteractions)
		{
			if (tileInteraction == null) continue;
			if (tileInteraction.WillInteract(interaction, NetworkSide.Server))
			{
				//perform if not on cooldown
				if (Cooldowns.TryStartServer(interaction, CommonCooldowns.Instance.Interaction))
				{
					tileInteraction.ServerPerformInteraction(interaction);
				}
				else
				{
					//hit a cooldown, rollback in case client tried to predict it
					tileInteraction.ServerRollbackClient(interaction);
				}

				// interaction should've triggered and did or we hit a cooldown, so we're
				// done processing this request.
				break;
			}
			else
			{
				tileInteraction.ServerRollbackClient(interaction);
			}
		}
	}

	public bool Interact(MouseDrop interaction)
	{
		Logger.Log("Interaction detected on InteractableTiles.");

		LayerTile tile = LayerTileAt(interaction.ShadowWorldLocation, true);

		if(tile is BasicTile basicTile)
		{
			var tileApply = new TileApply(interaction.Performer, interaction.UsedObject, interaction.Intent, (Vector2Int)WorldToCell(interaction.ShadowWorldLocation), this, basicTile, null, -((Vector2)interaction.Performer.transform.position - interaction.ShadowWorldLocation), TileApply.ApplyType.MouseDrop);
			var tileMouseDrop = new TileMouseDrop(interaction.Performer, interaction.UsedObject, interaction.Intent, (Vector2Int)WorldToCell(interaction.ShadowWorldLocation), this, basicTile, -((Vector2)interaction.Performer.transform.position - interaction.ShadowWorldLocation));
			foreach (var tileInteraction in basicTile.TileInteractions)
			{
				if (tileInteraction == null) continue;
				if (tileInteraction.WillInteract(tileApply, NetworkSide.Client) &&
					Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction))
				{
					//request the tile interaction because we think one will happen
					RequestInteractMessage.SendTileMouseDrop(tileMouseDrop, this);
					return true;
				}
			}
		}

		return false;
	}

	public static bool instanceActive = false;
	public void OnHoverStart()
	{
		OnHover();
	}

	public void OnHover()
	{
		var wallMount = CheckWallMountOverlay();
		if (wallMount)
		{
			Vector2 cameraPos = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
			var tilePos = cameraPos.RoundToInt();
			OrientationEnum orientation = OrientationEnum.Down;
			Vector3Int PlaceDirection = PlayerManager.LocalPlayerScript.WorldPos - tilePos;
			bool isWallBlocked = false;
			if (PlaceDirection.x != 0 && !MatrixManager.IsWallAt(tilePos + new Vector3Int(PlaceDirection.x > 0 ? 1 : -1, 0, 0), true))
			{
				if (PlaceDirection.x > 0)
				{
					orientation = OrientationEnum.Right;
				}
				else
				{
					orientation = OrientationEnum.Left;
				}
			}
			else
			{
				if (PlaceDirection.y != 0 && !MatrixManager.IsWallAt(tilePos + new Vector3Int(0, PlaceDirection.y > 0 ? 1 : -1, 0), true))
				{
					if (PlaceDirection.y > 0)
					{
						orientation = OrientationEnum.Up;
					}
					else
					{
						orientation = OrientationEnum.Down;
					}
				}
				else
				{
					isWallBlocked = true;
				}
			}

			if (!MatrixManager.IsWallAt(tilePos, false) || isWallBlocked)
			{
				if (instanceActive)
				{
					instanceActive = false;
					Highlight.DeHighlight();
				}
				return;
			}

			if (!instanceActive)
			{
				instanceActive = true;
				Highlight.ShowHighlight(UIManager.Hands.CurrentSlot.ItemObject, true);
			}

			Vector3 spritePos = tilePos;
			if (wallMount.IsWallProtrusion) //for light bulbs, tubes, cameras, etc. move the sprite towards the floor
			{
				if(orientation == OrientationEnum.Right)
				{
					spritePos.x += 0.5f;
					Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,270);
				}
				else if(orientation == OrientationEnum.Left)
				{
						spritePos.x -= 0.5f;
						Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,90);
				}
				else if(orientation == OrientationEnum.Up)
				{
					spritePos.y += 0.5f;
					Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,0);
				}
				else
				{
					spritePos.y -= 0.5f;
					Highlight.instance.spriteRenderer.transform.rotation = Quaternion.Euler(0,0,0);
				}
			}
			Highlight.instance.spriteRenderer.transform.position = spritePos;
		}
	}

	WallMountHandApplySpawn CheckWallMountOverlay()
	{
		var itemSlot = UIManager.Hands.CurrentSlot;
		if (itemSlot == null || itemSlot.ItemObject == null)
		{
			return null;
		}
		var wallMount = itemSlot.ItemObject.GetComponent<WallMountHandApplySpawn>();
		return wallMount;
	}

	public void OnHoverEnd()
	{
		if (instanceActive)
		{
			instanceActive = false;
			Highlight.DeHighlight();
		}
	}
}
