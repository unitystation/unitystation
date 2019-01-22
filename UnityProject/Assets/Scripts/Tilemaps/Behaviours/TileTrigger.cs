using UnityEngine;
using UnityEngine.Tilemaps;

public class TileTrigger : InputTrigger
{
	private MetaTileMap metaTileMap;
	private ObjectLayer objectLayer;

	private Tilemap floorTileMap;
	private Tilemap baseTileMap;
	private Tilemap wallTileMap;
	private Tilemap windowTileMap;
	private Tilemap objectTileMap;

	private Tilemap grillTileMap;

	void Start()
	{
		CacheTileMaps();
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

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		return DetermineTileAction(originator, position, hand);
	}

	private bool DetermineTileAction(GameObject originator, Vector3 position, string hand)
	{
		metaTileMap = originator.GetComponentInParent<MetaTileMap>();
		objectLayer = originator.GetComponentInParent<ObjectLayer>();
		PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();

		Vector3Int pos = objectLayer.transform.InverseTransformPoint(position).RoundToInt();
		pos.z = 0;
		Vector3Int cellPos = baseTileMap.WorldToCell(position);

		LayerTile tile = metaTileMap.GetTile(pos);

		GameObject handObj = UIManager.Hands.CurrentSlot.Item;

		// Nothing in hand, do nothing
		if (handObj == null)
		{
			return false;
		}

		if (tile != null)
		{
			switch (tile.TileType)
			{
				case TileType.Table:
				{
					Vector3 targetPosition = position;
					targetPosition.z = -0.2f;
					pna.CmdPlaceItem(hand, targetPosition, originator, true);
					return true;
				}
				case TileType.Floor:
				{
					//Crowbar
					if (handObj.GetComponent<CrowbarTrigger>())
					{
						pna.CmdCrowBarRemoveFloorTile(originator, LayerType.Floors,
							new Vector2(cellPos.x, cellPos.y), position);

						return true;
					}

					break;
				}
				case TileType.Base:
				{
					if (handObj.GetComponent<UniFloorTile>())
					{
						pna.CmdPlaceFloorTile(originator,
							new Vector2(cellPos.x, cellPos.y), handObj);

						return true;
					}

					break;
				}
				case TileType.Window:
				case TileType.Grill:
				{
					//Check Melee:
					MeleeTrigger melee = grillTileMap.gameObject.GetComponent<MeleeTrigger>();
					if (melee != null && melee.MeleeInteract(originator, hand))
					{
						return true;
					}

					break;
				}
				case TileType.Wall:
				{
					Welder welder = handObj.GetComponent<Welder>();
					if (welder)
					{
						if (welder.isOn)
						{
							//Request to deconstruct from the server:
							RequestTileDeconstructMessage.Send(originator, gameObject, TileType.Wall,
								cellPos, position);

							return true;
						}
					}

					break;
				}
			}
		}

		return false;
	}
}