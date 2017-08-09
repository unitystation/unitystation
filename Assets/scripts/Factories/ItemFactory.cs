using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemFactory : MonoBehaviour {

	private static ItemFactory itemFactory;
	public static ItemFactory Instance{
		get{ 
			if (itemFactory == null) {
				itemFactory = FindObjectOfType<ItemFactory>();
				Instance.Init();
			}
			return itemFactory;
		}
	}

	private GameObject fireTile{ get; set; }
	private GameObject scorchMarksTile{ get; set; }
	private GameObject shroudTile{ get; set; }

	//Parents to make tidy
	private GameObject shroudParent;

	void Init(){
		//Do init stuff
		Instance.fireTile = Resources.Load("FireTile") as GameObject;
		Instance.scorchMarksTile = Resources.Load("ScorchMarks") as GameObject;
		Instance.shroudTile = Resources.Load("ShroudTile") as GameObject;

		//Parents
		Instance.shroudParent =  new GameObject();
		Instance.shroudParent.name = "FieldOfView(Shrouds)";
	}

	//FileTiles are client side effects only, no need for network sync (triggered by same event on all clients/server)
	public void SpawnFileTile(float fuelAmt, Vector2 position){
		//ClientSide pool spawn
		GameObject fireObj = PoolManager.PoolClientInstantiate(Instance.fireTile, position, Quaternion.identity);
		FireTile fT = fireObj.GetComponent<FireTile>();
		fT.StartFire(fuelAmt);
	}

	public GameObject SpawnScorchMarks(Transform parent){
	//ClientSide spawn
		GameObject sM = PoolManager.PoolClientInstantiate(Instance.scorchMarksTile, parent.position, Quaternion.identity);
		sM.transform.parent = parent;
		return sM;
	}

	public GameObject SpawnShroudTile(Vector3 pos){
		GameObject sT = PoolManager.PoolClientInstantiate(Instance.shroudTile, pos, Quaternion.identity);
		sT.transform.parent = shroudParent.transform;
		return sT;
	}
}
