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
		}

		void Start()
		{
			snapControl = GetComponent<EditModeControl>();
		}

		//receive broadcast message when item is dropped from hand
		public void OnRemoveFromInventory()
		{
			if (snapControl != null) {
				snapControl.Snap();
			}
		}

	}
} 