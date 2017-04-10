using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using PlayGroup;

namespace Items
{
	[RequireComponent(typeof(NetworkIdentity))]
	public class ItemNetwork: NetworkBehaviour
	{
		private bool synced = false;
		private EditModeControl snapControl;

		void Start()
		{
			snapControl = GetComponent<EditModeControl>();
		}

		public void SnapToGrid(){
			if (snapControl != null) {
				snapControl.Snap();
			}
		}
		//receive broadcast message when item is dropped from hand (client side)
		public void OnRemoveFromInventory()
		{


		}

	}
} 