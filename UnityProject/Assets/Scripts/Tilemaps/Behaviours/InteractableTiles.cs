using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Component which allows all the tiles of a matrix to be interacted with.
/// </summary>
public class InteractableTiles : MonoBehaviour, IInteractable<PositionalHandApply>
{
	private MetaTileMap metaTileMap;
	private ObjectLayer objectLayer;

	private Tilemap floorTileMap;
	private Tilemap baseTileMap;
	private Tilemap wallTileMap;
	private Tilemap windowTileMap;
	private Tilemap objectTileMap;

	private Tilemap grillTileMap;

	private void Start()
	{
		CacheTileMaps();
	}

	public bool Interact(PositionalHandApply interaction)
	{
		if (!DefaultWillInteract.PositionalHandApply(interaction, NetworkSide.Client)) return false;

		metaTileMap = interaction.Performer.GetComponentInParent<MetaTileMap>();
		objectLayer = interaction.Performer.GetComponentInParent<ObjectLayer>();
		PlayerNetworkActions pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		Vector3Int pos = objectLayer.transform.InverseTransformPoint(interaction.WorldPositionTarget).RoundToInt();
		pos.z = 0;
		Vector3Int cellPos = baseTileMap.WorldToCell(interaction.WorldPositionTarget);

		LayerTile tile = metaTileMap.GetTile(pos);

		if (tile != null)
		{
			switch (tile.TileType)
			{
				case TileType.Table:
				{
					Vector3 targetPosition = interaction.WorldPositionTarget;
					targetPosition.z = -0.2f;
					pna.CmdPlaceItem(interaction.HandSlot.equipSlot, targetPosition, interaction.Performer, true);
					return true;
				}
				case TileType.Floor:
				{
					//Crowbar
					if (Validations.IsTool(interaction.HandObject, ToolType.Crowbar))
					{
						pna.CmdCrowBarRemoveFloorTile(interaction.Performer, LayerType.Floors,
							new Vector2(cellPos.x, cellPos.y), interaction.WorldPositionTarget);
						return true;
					}

					break;
				}
				case TileType.Base:
				{
					if (Validations.HasComponent<UniFloorTile>(interaction.HandObject))
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
					Meleeable melee = windowTileMap.gameObject.GetComponent<Meleeable>();
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
							RequestTileDeconstructMessage.Send(interaction.Performer, gameObject, TileType.Wall,
								cellPos, interaction.WorldPositionTarget);
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
		var tilemaps = GetComponentsInChildren<Tilemap>(true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains("Floors"))
			{
				floorTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Base"))
			{
				baseTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Walls"))
			{
				wallTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Windows"))
			{
				windowTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Objects"))
			{
				objectTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains("Grills"))
			{
				grillTileMap = tilemaps[i];
			}
		}
	}


}