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

	//cached - can be static since there is no state-dependent validation
	private static InteractionValidationChain<PositionalHandApply> validations;

	void Start()
	{
		CacheTileMaps();
		if (validations == null)
		{
			validations = InteractionValidationChain<PositionalHandApply>.Create()
				.WithValidation(CanApply.ONLY_IF_CONSCIOUS)
				.WithValidation(IsHand.OCCUPIED);
		}
	}

	public InteractionControl Interact(PositionalHandApply interaction)
	{
		if (!validations.DoesValidate(interaction, NetworkSide.CLIENT))
		{
			return InteractionControl.CONTINUE_PROCESSING;
		}
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
					pna.CmdPlaceItem(interaction.HandSlot.SlotName, targetPosition, interaction.Performer, true);
					return InteractionControl.STOP_PROCESSING;
				}
				case TileType.Floor:
				{
					//Crowbar
					var tool = interaction.UsedObject.GetComponent<Tool>();
					if (tool != null && tool.ToolType == ToolType.Crowbar)
					{
						pna.CmdCrowBarRemoveFloorTile(interaction.Performer, LayerType.Floors,
							new Vector2(cellPos.x, cellPos.y), interaction.WorldPositionTarget);
						return InteractionControl.STOP_PROCESSING;
					}

					break;
				}
				case TileType.Base:
				{
					if (interaction.UsedObject.GetComponent<UniFloorTile>())
					{
						pna.CmdPlaceFloorTile(interaction.Performer,
							new Vector2(cellPos.x, cellPos.y), interaction.UsedObject);
						return InteractionControl.STOP_PROCESSING;
					}

					break;
				}
				case TileType.Window:
				{
					//Check Melee:
					MeleeTrigger melee = windowTileMap.gameObject.GetComponent<MeleeTrigger>();
					if (melee != null && melee.MeleeInteract(interaction.Performer, interaction.HandSlot.SlotName))
					{
						return InteractionControl.STOP_PROCESSING;
					}

					break;
				}
				case TileType.Grill:
				{
					//Check Melee:
					MeleeTrigger melee = grillTileMap.gameObject.GetComponent<MeleeTrigger>();
					if (melee != null && melee.MeleeInteract(interaction.Performer, interaction.HandSlot.SlotName))
					{
						return InteractionControl.STOP_PROCESSING;
					}

					break;
				}
				case TileType.Wall:
				{
					Welder welder = interaction.UsedObject.GetComponent<Welder>();
					if (welder)
					{
						if (welder.isOn)
						{
							//Request to deconstruct from the server:
							RequestTileDeconstructMessage.Send(interaction.Performer, gameObject, TileType.Wall,
								cellPos, interaction.WorldPositionTarget);
							return InteractionControl.STOP_PROCESSING;
						}
					}
					break;
				}
			}
		}

		return InteractionControl.CONTINUE_PROCESSING;
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