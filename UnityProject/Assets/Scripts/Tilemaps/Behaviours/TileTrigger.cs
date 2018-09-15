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

	void Start(){
		CacheTileMaps();
	}

	void CacheTileMaps ()
	{
		var tilemaps = GetComponentsInChildren<Tilemap> (true);
		for (int i = 0; i < tilemaps.Length; i++)
		{
			if (tilemaps[i].name.Contains ("Floors"))
			{
				floorTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Base"))
			{
				baseTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Walls"))
			{
				wallTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Windows"))
			{
				windowTileMap = tilemaps[i];
			}

			if (tilemaps[i].name.Contains ("Objects"))
			{
				objectTileMap = tilemaps[i];
			}
		}
	}

	public override void Interact (GameObject originator, Vector3 position, string hand)
	{
		metaTileMap = originator.GetComponentInParent<MetaTileMap> ();
		objectLayer = originator.GetComponentInParent<ObjectLayer> ();

		Vector3Int pos = objectLayer.transform.InverseTransformPoint (position).RoundToInt ();
		pos.z = 0;

		LayerTile tile = metaTileMap.GetTile (pos);

		if (tile?.TileType == TileType.Table)
		{
			TableInteraction interaction = new TableInteraction (gameObject, originator, position, hand);

			interaction.Interact (isServer);
		}

		if (tile?.TileType == TileType.Floor)
		{
			var handObj = UIManager.Hands.CurrentSlot.Item;

			if(handObj == null)
			{
				return;
			}

			//Crowbar
			if (handObj.GetComponent<CrowbarTrigger>())
			{
				var pna = originator.GetComponent<PlayerNetworkActions>();
				var cellPos = floorTileMap.WorldToCell(position);
				pna.CmdCrowBarRemoveFloorTile(transform.root.gameObject, TileChangeLayer.Floor,
				new Vector2(cellPos.x, cellPos.y), position);
			}
		}
	}
}