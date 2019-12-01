using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Component which allows all the tiles of a matrix to be interacted with.
/// </summary>
public class InteractableTiles : MonoBehaviour, IClientInteractable<PositionalHandApply>
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

	public bool Interact(PositionalHandApply interaction)
	{ //todo: refactor to IF2!
		if (!DefaultWillInteract.PositionalHandApply(interaction, NetworkSide.Client)) return false;

		PlayerNetworkActions pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		Vector3Int pos = objectLayer.transform.InverseTransformPoint(interaction.WorldPositionTarget).RoundToInt();
		pos.z = 0;
		Vector3Int cellPos = baseLayer.WorldToCell(interaction.WorldPositionTarget);

		LayerTile tile = LayerTileAt(interaction.WorldPositionTarget);

		if (tile != null)
		{
			switch (tile.TileType)
			{
				case TileType.Table:
				{
					Vector3 targetPosition = interaction.WorldPositionTarget;
					targetPosition.z = -0.2f;
					pna.CmdPlaceItem(interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none),
						targetPosition, interaction.Performer, true);
					return true;
				}
				case TileType.Floor:
				{
					//Crowbar
					if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
					{
						pna.CmdCrowBarRemoveFloorTile(interaction.Performer, LayerType.Floors,
							new Vector2(cellPos.x, cellPos.y), interaction.WorldPositionTarget);
						return true;
					}

					break;
				}
				case TileType.Base:
				{
					if (Validations.HasComponent<PlaceableTile>(interaction.HandObject))
					{
						pna.CmdPlaceFloorTile(interaction.Performer,
							new Vector2(cellPos.x, cellPos.y), interaction.HandObject);
						return true;
					}

					break;
				}
				case TileType.Window:
				{
					//Check Melee:
					Meleeable melee = windowLayer.gameObject.GetComponent<Meleeable>();
					if (melee != null &&
					    melee.Interact(PositionalHandApply.ByLocalPlayer(gameObject)))
					{
						return true;
					}

					break;
				}
				case TileType.Grill:
				{
					//Check Melee:
					Meleeable melee = grillTileMap.gameObject.GetComponent<Meleeable>();
					if (melee != null && melee.Interact(PositionalHandApply.ByLocalPlayer(gameObject)))
					{
						return true;
					}

					break;
				}
				case TileType.Wall:
				{
					Welder welder = interaction.HandObject != null ? interaction.HandObject.GetComponent<Welder>() : null;
					if (welder != null)
					{
						if (welder.isOn)
						{
							//Request to deconstruct from the server:
							RequestTileDeconstructMessage.Send(interaction.Performer, gameObject, TileType.Wall, interaction.WorldPositionTarget);
							return true;
						}
					}
					break;
				}
			}
		}

		return false;
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