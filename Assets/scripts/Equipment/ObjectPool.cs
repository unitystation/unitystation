using System;
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
	private bool IsWaiting;
	private bool AbortDelayedDrop;

	public void AddGameObject(GameObject obj){
			obj.transform.position = transform.position;
			obj.transform.parent = transform;
//			lastSync = Time.time;
	
			NetworkIdentity id = obj.GetComponent<NetworkIdentity>();
			ItemAttributes att = obj.GetComponent<ItemAttributes>();

			if (currentObjects.ContainsKey(id)) {
				currentObjects.Remove(id);
				currentObjects.Add(id, att);
			} else {
				currentObjects.Add(id, att);
			}
		}

	public void DestroyGameObject(GameObject gObj)
	{
		DropGameObject(gObj, Vector3.zero);
	}

//	private float lastSync;
	//When dropping items etc, remove them from the player equipment pool and place in scene
		public void DropGameObject(GameObject gObj, Vector3 dropPos)
		{
			NetworkIdentity id = gObj.GetComponent<NetworkIdentity>();
			if ( !currentObjects.ContainsKey(id) )
			{
				Debug.Log("item: " + gObj.name + "was not found in Player Equipment pool");
			}
			else
			{
				if ( !dropPos.Equals(Vector3.zero) )
				{
					var o = currentObjects[id].gameObject;
//					var delta = Time.time - lastSync;
//					if ( delta > 0.25f )
//					{
						DropNow(o, dropPos);
//					}
//					else
//					{
//						StartCoroutine(DropWait(o, dropPos, delta));
//					}
					
				}
				currentObjects.Remove(id);
			}
		}

//	private IEnumerator DropWait(GameObject gObj, Vector3 dropPos, float waitTime)
//	{
//		AbortDelayedDrop = false;
//		if (!IsWaiting)
//		{
//			IsWaiting = true;
//			Debug.LogFormat("DropWait {0}", waitTime);
//			yield return new WaitForSeconds(waitTime);
//			IsWaiting = false;
//			if(!AbortDelayedDrop) DropNow(gObj, dropPos);
//		}
//		else
//		{
//			AbortDelayedDrop = true;
//			DropNow(gObj, dropPos);
//		}
//	}

	private void DropNow(GameObject gObj, Vector3 dropPos)
	{
		gObj.transform.parent = null;
		gObj.transform.position = dropPos;
		EditModeControl e = gObj.GetComponent<EditModeControl>();
		e.Snap();
//		lastSync = Time.time;
	}
}
}
