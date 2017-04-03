using Cupboards;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Network
{

	public class DoorSync: NetworkBehaviour
	{
		private bool synced = false;

		private ClosetControl closetControl;
		private LockLightController lockLight;
		private GameObject items;

		void Start()
		{
			closetControl = GetComponent<ClosetControl>();
			lockLight = transform.GetComponentInChildren<LockLightController>();
			items = transform.FindChild("Items").gameObject;
			StartSync();
		}

		void OnConnectedToServer()
		{
			CmdSendCurrentState(netId);
		}
		//Sync
		[Command]
		void CmdSendCurrentState(NetworkInstanceId playerRequesting)
		{
			NetworkIdentity[] objectsInCupB = items.GetComponentsInChildren<NetworkIdentity>();
			if (objectsInCupB != null) {
				foreach (NetworkIdentity p in objectsInCupB) {
					closetControl.RpcDropItem(p.netId);
				}
			}

			if (lockLight != null) {
				RpcReceiveCurrentState(playerRequesting, lockLight.IsLocked(), closetControl.IsClosed, transform.parent.transform.position);
			} else {
				RpcReceiveCurrentState(playerRequesting, false, closetControl.IsClosed, transform.parent.transform.position);
			}
		}

		[ClientRpc]
		void RpcReceiveCurrentState(NetworkInstanceId playerIdent, bool isLocked, bool isClosed, Vector3 pos)
		{
			if (netId == playerIdent) {

				if (isClosed) {
					closetControl._Close();
				} else {
					closetControl._Open();
				}

				if (lockLight != null) {
					if (isLocked) {
						lockLight.Lock();
					} else {
						lockLight.Unlock();
					}
				}

				if (transform.parent.transform.position != pos) { //Position of cupboard
					transform.parent.transform.position = pos;
				}
			}
		}
			
		void StartSync()
		{
			if (!synced) {
				if (!isServer) {
					//If you are not the master then update the current IG state of this object from the master
					CmdSendCurrentState(netId);
				}

				synced = true;
			}
		}
	}
}