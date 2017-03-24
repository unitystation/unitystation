using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using PlayGroup;
using Network;

namespace Items
{
	[RequireComponent(typeof(NetworkIdentity))]
	public class ItemNetwork: NetworkBehaviour
	{
		private bool synced = false;
		private EditModeControl snapControl;

		public void OnAddToInventory(string slotName)
		{
			CmdRequestOwnership(GetComponent<NetworkIdentity>());
		}

		void Start()
		{
			snapControl = GetComponent<EditModeControl>();
				//Has been instantiated at runtime and you received instantiate of this object from photon on room join
				StartSync();
		}

		//receive broadcast message when item is dropped from hand
		public void OnRemoveFromInventory()
		{
			if (snapControl != null) {
				snapControl.Snap();
			}
		}

		void OnConnectedToServer()
		{
			StartSync();
		}

		void StartSync()
		{
			NetworkItemDB.AddItem(netId, gameObject);
			synced = true;
		}

		[Command]
		void CmdRequestOwnership(NetworkIdentity requestor)
		{
//			var nIdentity = GetComponent<NetworkIdentity>();
//			nIdentity.AssignClientAuthority(requestor.connectionToClient);
			Debug.Log("TODO: RequestOwnership");
		}


	}
} 