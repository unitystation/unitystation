using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


	//For items that are the ownership of players, the items are kept in a pool serverside and sprites references
	//sent to the client UI and playerobj
	public class ObjectPool : MonoBehaviour
	{
		public Dictionary<NetworkIdentity, ItemAttributes> currentObjects =
			new Dictionary<NetworkIdentity, ItemAttributes>();

		/// Owner player
		public PlayerScript Owner { get; set; }

		public void AddGameObject(GameObject obj)
		{
			var objTransform = obj.GetComponent<CustomNetTransform>();
			objTransform.DisappearFromWorldServer();

			NetworkIdentity id = obj.GetComponent<NetworkIdentity>();
			ItemAttributes att = obj.GetComponent<ItemAttributes>();

			if (currentObjects.ContainsKey(id))
			{
				currentObjects.Remove(id);
				currentObjects.Add(id, att);
			}
			else
			{
				currentObjects.Add(id, att);
			}
		}

		public void DestroyGameObject(GameObject gObj)
		{
			DropGameObject(gObj, TransformState.HiddenPos);
		}

		//When dropping items etc, remove them from the player equipment pool and place in scene
		public void DropGameObject(GameObject gObj, Vector3 dropPos)
		{
			NetworkIdentity id = gObj.GetComponent<NetworkIdentity>();
			if (!currentObjects.ContainsKey(id))
			{
				Logger.Log("item: " + gObj.name + "was not found in Player Equipment pool", Category.Equipment);
			}
			else
			{
				if (dropPos != TransformState.HiddenPos)
				{
					GameObject o = currentObjects[id].gameObject;
					DropNow(o, dropPos);
				}
				currentObjects.Remove(id);
				gObj.GetComponent<RegisterTile>().UpdatePosition();
			}
		}

		private void DropNow(GameObject gObj, Vector3 dropPos)
		{
			var objTransform = gObj.GetComponent<CustomNetTransform>();
			if ( Owner ) {
				//Inertia drop works only if player has external impulse (space floating etc.)
				objTransform.InertiaDrop( dropPos, Owner.playerMove.speed, Owner.PlayerSync.ServerState.Impulse );
			} else {
				objTransform.AppearAtPositionServer(dropPos); 
			}
		}
	}
