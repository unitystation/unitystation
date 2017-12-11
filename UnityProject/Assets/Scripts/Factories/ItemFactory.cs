using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AccessType;

public class ItemFactory : MonoBehaviour
{
	public static ItemFactory Instance;

	/* Example:
	private GameObject someItem{ get; set; }
    */

	private GameObject idCard { get; set; }

	void Awake()
	{
		if (Instance == null) {
			Instance = this;
		} else {
			Destroy(this);
		}
	}

	void Start()
	{
		/* Example:
		Instance.someItem = Resources.Load("SomeItem") as GameObject;
		*/
		idCard = Resources.Load("ID") as GameObject;
	}

	/* Example Spawn:

	//Only client side spawn, not network. For things that are just eye candy like bullets, sparks etc
 	public void SpawnSomeItem(float ItemStatAmt, Vector2 position){
		
		GameObject itemObj = PoolManager.PoolClientInstantiate(Instance.someItem, position, Quaternion.identity);
		ItemThingy iT = itemObj.GetComponent<ItemThingy>();
		iT.DoStuff(ItemStatAmt);
	}

	Example of a network pool spawn:
	GameObject networkedObj = PoolManager.PoolNetworkInstantiate(prefabObj, Vector2.zero, Quaternion.identity);
	*/

	/// <summary>
	/// For spawning new ID cards, mostly used on new player spawn
	/// </summary>
	public GameObject SpawnIDCard(IDCardType idCardType, JobType jobType, List<Access> allowedAccess, string name)
	{
		//No need to pool it but doesn't hurt (and requires less lines)
		GameObject idObj = PoolManager.Instance.PoolNetworkInstantiate(idCard, Vector2.zero, Quaternion.identity);
		IDCard ID = idObj.GetComponent<IDCard>();

		//Set all the synced properties for the card
		ID.RegisteredName = name;
		ID.jobTypeInt = (int)jobType;
		ID.idCardTypeInt = (int)idCardType;
		ID.AddAccessList(allowedAccess);
		return idObj;
	}
}
