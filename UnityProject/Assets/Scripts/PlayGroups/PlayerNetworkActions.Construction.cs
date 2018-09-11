using UnityEngine;
using UnityEngine.Networking;

public partial class PlayerNetworkActions : NetworkBehaviour
{
	[Command]
	public void CmdCrowBarRemoveFloorTile (GameObject tileChangeRoot,
		TileChangeLayer layer, Vector2 cellPos)
	{
		TileChangeManager tm = tileChangeRoot.GetComponent<TileChangeManager> ();
		if (tm == null)
		{
			Debug.LogError ("TileChangeManager not found");
			return;
		}

		tm.RemoveTile(Vector2Int.RoundToInt(cellPos), layer);
	}
}