using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Items;
using UI;

namespace Equipment{
	//For items that are the ownership of players, the items are kept in a pool serverside and sprites references
	//sent to the client UI and playerobj
public class ObjectPool : MonoBehaviour {

		public Dictionary<NetworkIdentity,ItemAttributes> currentObjects = new Dictionary<NetworkIdentity, ItemAttributes>();

		public void AddGameObject(GameObject obj){
			obj.transform.position = transform.position;
			obj.transform.parent = transform;
			NetworkIdentity id = obj.GetComponent<NetworkIdentity>();
			ItemAttributes att = obj.GetComponent<ItemAttributes>();

			if (currentObjects.ContainsKey(id)) {
				currentObjects.Remove(id);
				currentObjects.Add(id, att);
			} else {
				currentObjects.Add(id, att);
			}
		}

		//When dropping items etc, remove them from the player equipment pool and place in scene
		public void DropGameObject(GameObject gObj, Vector3 pos){
			NetworkIdentity id = gObj.GetComponent<NetworkIdentity>();
            if (currentObjects.ContainsKey(id))
            {
                currentObjects[id].gameObject.transform.parent = null;
                currentObjects[id].gameObject.transform.position = pos;
				EditModeControl e = gObj.GetComponent<EditModeControl>();
                e.Snap();
                currentObjects.Remove(id);
            }
            else
            {
                Debug.Log("item: " + gObj.name + "was not found in Player Equipment pool");
            }
		}
}
}
