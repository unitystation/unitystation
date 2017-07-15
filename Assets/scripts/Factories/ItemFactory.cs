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

	void Init(){
		//Do init stuff
		Instance.fireTile = Resources.Load("FireTile") as GameObject;
		Instance.scorchMarksTile = Resources.Load("ScorchMarks") as GameObject;
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
}
