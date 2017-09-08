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

	/* Example:
	private GameObject someItem{ get; set; }
	
    */
	void Init(){
	/* Example:
	Instance.someItem = Resources.Load("SomeItem") as GameObject;
	
    */
	}

	/* Example Spawn:

	//Only client side spawn, not network
 	public void SpawnSomeItem(float ItemStatAmt, Vector2 position){
		
		GameObject itemObj = PoolManager.PoolClientInstantiate(Instance.someItem, position, Quaternion.identity);
		ItemThingy iT = itemObj.GetComponent<ItemThingy>();
		iT.DoStuff(ItemStatAmt);
	}
	*/
}
