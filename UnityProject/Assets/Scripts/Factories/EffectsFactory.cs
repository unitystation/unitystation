using Sprites;
using UnityEngine;
using UnityEngine.Networking;

public class EffectsFactory : NetworkBehaviour
{
	public static EffectsFactory Instance;

	//Parents to make tidy
	private GameObject shroudParent;

	private GameObject fireTile { get; set; }
	private GameObject scorchMarksTile { get; set; }
	private GameObject shroudTile { get; set; }

	private GameObject bloodTile { get; set; }

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
		scorchMarksTile = Resources.Load("ScorchMarks") as GameObject;
		shroudTile = Resources.Load("ShroudTile") as GameObject;
		bloodTile = Resources.Load("BloodSplat") as GameObject;
		//Parents
		shroudParent = new GameObject();
		shroudParent.transform.position += new Vector3(0.5f, 0.5f, 0);
		shroudParent.name = "FieldOfView(Shrouds)";
	}

	//FileTiles are client side effects only, no need for network sync (triggered by same event on all clients/server)
	public void SpawnFileTileLocal(float fuelAmt, Vector3 localPosition, Transform parent)
	{
		//ClientSide pool spawn
		GameObject fireObj = PoolManager.Instance.PoolClientInstantiate(fireTile, Vector3.zero, Quaternion.identity);
		//Spawn tiles need to be placed in a local matrix:
		fireObj.transform.parent = parent;
		fireObj.transform.localPosition = localPosition;
		FireTile fT = fireObj.GetComponent<FireTile>();
		fT.StartFire(fuelAmt);
	}

	public GameObject SpawnScorchMarks(Transform parent)
	{
		//ClientSide spawn
		GameObject sM =
			PoolManager.Instance.PoolClientInstantiate(scorchMarksTile, parent.position, Quaternion.identity);
		sM.transform.parent = parent;
		return sM;
	}

	public GameObject SpawnShroudTile(Vector3 pos)
	{
		GameObject sT = PoolManager.Instance.PoolClientInstantiate(shroudTile, pos, Quaternion.identity);
		sT.transform.parent = shroudParent.transform;
		return sT;
	}

	[Server]
	public void BloodSplat(Vector3 pos, BloodSplatSize splatSize)
	{
		GameObject b = PoolManager.Instance.PoolNetworkInstantiate(bloodTile, pos, Quaternion.identity);
		BloodSplat bSplat = b.GetComponent<BloodSplat>();
		//choose a random blood sprite
		int spriteNum = 0;
		switch (splatSize)
		{
			case BloodSplatSize.small:
				spriteNum = Random.Range(137, 139);
				break;
			case BloodSplatSize.medium:
				spriteNum = Random.Range(116, 120);
				break;
			case BloodSplatSize.large:
				spriteNum = Random.Range(51, 56);
				break;
		}

		bSplat.bloodSprite = spriteNum;
	}
}