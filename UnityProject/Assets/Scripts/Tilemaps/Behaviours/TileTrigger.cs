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
		var pna = originator.GetComponent<PlayerNetworkActions>();

		Vector3Int pos = objectLayer.transform.InverseTransformPoint(position).RoundToInt();
		pos.z = 0;
		var cellPos = baseTileMap.WorldToCell(position);

		LayerTile tile = metaTileMap.GetTile(pos);

		var handObj = UIManager.Hands.CurrentSlot.Item;

		// Nothing in hand, do nothing
		if (handObj == null)
		{
			return false;
		}

		//determine what to do based on the type of tile being interacted with

		if (tile?.TileType == TileType.Table)
		{
			Vector3 targetPosition = position;
			targetPosition.z = -0.2f;
			pna.CmdPlaceItem(hand, targetPosition, transform.root.gameObject, true);
			return true;
		}
		else if (tile?.TileType == TileType.Floor)
		{
			//Crowbar
			if (handObj.GetComponent<CrowbarTrigger>())
			{
				pna.CmdCrowBarRemoveFloorTile(transform.root.gameObject, TileChangeLayer.Floor,
					new Vector2(cellPos.x, cellPos.y), position);
				return true;
			}
		}
		else if (tile?.TileType == TileType.Base)
		{
			if (handObj.GetComponent<UniFloorTile>())
			{
				pna.CmdPlaceFloorTile(transform.root.gameObject,
					new Vector2(cellPos.x, cellPos.y), handObj);
				return true;
			}
		}
		else if (tile?.TileType == TileType.Window)
		{
			//TODO: This might be better done in InputController
			//Check Melee:
			MeleeTrigger melee = windowTileMap.gameObject.GetComponent<MeleeTrigger>();
			if (melee != null && melee.MeleeInteract(originator, hand))
			{
				return true;
			}
		}
		else if (tile?.TileType == TileType.Grill)
		{
			//TODO: This might be better done in InputController
			//Check Melee:
			MeleeTrigger melee = grillTileMap.gameObject.GetComponent<MeleeTrigger>();
			if (melee != null && melee.MeleeInteract(originator, hand))
			{
				return true;
			}
		}
		else if (tile?.TileType == TileType.Wall)
		{
			var welder = handObj.GetComponent<Welder>();
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
		}

		return false;
	}
}