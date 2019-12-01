using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Component which allows all the tiles of a matrix to be interacted with.
/// </summary>
public class InteractableTiles : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
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
	public LayerTile LayerTileAt(Vector2 worldPos)
	{
		Vector3Int pos = objectLayer.transform.InverseTransformPoint(worldPos).RoundToInt();

		return metaTileMap.GetTile(pos);
	}


	/// <summary>
	/// Converts the world position to a cell position on these tiles.
	/// </summary>
	/// <param name="worldPosition"></param>
	/// <returns></returns>
	public Vector3Int WorldToCell(Vector2 worldPosition)
	{
		Vector3Int pos = objectLayer.transform.InverseTransformPoint(worldPosition).RoundToInt();
		pos.z = 0;
		return baseLayer.WorldToCell(worldPosition);
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.PositionalHandApply(interaction, side)) return false;

		LayerTile tile = LayerTileAt(interaction.WorldPositionTarget);
		//check for melee / deconstruction
		if (tile != null)
		{
			switch (tile.TileType)
			{
				case TileType.Table:
				{
					//place on table
					return interaction.HandObject != null;
				}
				case TileType.Base:
				case TileType.Floor:
				case TileType.Wall:
				case TileType.Window:
				case TileType.Grill:
				{
					//melee or deconstruct with item in hand
					return true;
				}
			}
		}

		return false;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		Vector3Int pos = objectLayer.transform.InverseTransformPoint(interaction.WorldPositionTarget).RoundToInt();
		pos.z = 0;
		LayerTile tile = LayerTileAt(interaction.WorldPositionTarget);
		if (tile != null)
		{
			switch (tile.TileType)
			{
				case TileType.Table:
				{
					//place item
					Inventory.ServerDrop(interaction.HandSlot, interaction.WorldPositionTarget);
					break;
				}
				case TileType.Base:
				case TileType.Floor:
				case TileType.Wall:
				case TileType.Window:
				case TileType.Grill:
				{
					//possibly deconstruct
					if (tile is BasicTile basicTile)
					{
						if (basicTile.CanDeconstruct(interaction))
						{
							basicTile.ServerDeconstruct(interaction);
							break;
						}
					}
					//melee if we didn't deconstruct
					// Direction of attack towards the attack target.
					Vector2 dir = ((Vector3)interaction.WorldPositionTarget - interaction.Performer.WorldPosServer())
						.normalized;
					var wna = interaction.Performer.GetComponent<WeaponNetworkActions>();
					if (interaction.HandObject == null)
					{
						wna.CmdRequestPunchAttack(gameObject, dir, BodyPartType.None);
					}
					else
					{
						wna.CmdRequestMeleeAttack(gameObject, interaction.HandObject, dir, BodyPartType.None, tile.LayerType);
					}
					break;
				}
			}
		}
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


}