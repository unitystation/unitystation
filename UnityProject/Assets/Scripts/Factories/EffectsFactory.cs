using UnityEngine;
using UnityEngine.Networking;

public class EffectsFactory : NetworkBehaviour
{
	public static EffectsFactory Instance;

	private GameObject fireTile { get; set; }

	private GameObject smallBloodTile;
	private GameObject mediumBloodTile;
	private GameObject largeBloodTile;
	private GameObject largeAshTile;
	private GameObject smallAshTile;
	private GameObject waterTile { get; set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(this);
		}
	}

	private void Start()
	{
		//Do init stuff
		fireTile = Resources.Load("FireTile") as GameObject;
		smallBloodTile = Resources.Load("SmallBloodSplat") as GameObject;
		mediumBloodTile = Resources.Load("MediumBloodSplat") as GameObject;
		largeBloodTile = Resources.Load("LargeBloodSplat") as GameObject;
		largeAshTile = Resources.Load("LargeAsh") as GameObject;
		smallAshTile = Resources.Load("SmallAsh") as GameObject;
		waterTile = Resources.Load("WaterSplat") as GameObject;
	}

	//FileTiles are client side effects only, no need for network sync (triggered by same event on all clients/server)
	public void SpawnFireTileClient(float fuelAmt, Vector3 localPosition, Transform parent)
	{
		//ClientSide pool spawn
		GameObject fireObj = PoolManager.PoolClientInstantiate(fireTile, Vector3.zero);
		//Spawn tiles need to be placed in a local matrix:
		fireObj.transform.parent = parent;
		fireObj.transform.localPosition = localPosition;
		FireTile fT = fireObj.GetComponent<FireTile>();
		fT.StartFire(fuelAmt);
	}

	[Server]
	public void BloodSplat(Vector3 worldPos, BloodSplatSize splatSize)
	{
		GameObject chosenTile = null;
		switch (splatSize)
		{
			case BloodSplatSize.small:
				chosenTile = smallBloodTile;
				break;
			case BloodSplatSize.medium:
				chosenTile = mediumBloodTile;
				break;
			case BloodSplatSize.large:
				chosenTile = largeBloodTile;
				break;
		}

		if (chosenTile != null)
		{
			PoolManager.PoolNetworkInstantiate(chosenTile, worldPos,
				MatrixManager.AtPoint(Vector3Int.RoundToInt(worldPos), true).Objects);
		}
	}

	/// <summary>
	/// Creates ash at the specified tile position
	/// </summary>
	/// <param name="worldTilePos"></param>
	/// <param name="large">if true, spawns the large ash pile, otherwise spawns the small one</param>
	public void Ash(Vector2Int worldTilePos, bool large)
	{
		PoolManager.PoolNetworkInstantiate(large ? largeAshTile : smallAshTile, worldTilePos.To3Int(),
			MatrixManager.AtPoint(worldTilePos.To3Int(), true).Objects);
	}

	[Server]
	public void WaterSplat(Vector3 worldPos)
	{
		PoolManager.PoolNetworkInstantiate(waterTile, worldPos,
			MatrixManager.AtPoint(Vector3Int.RoundToInt(worldPos), true).Objects, Quaternion.identity);
	}
}