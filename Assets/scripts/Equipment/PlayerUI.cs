using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Events;
using UI;

namespace Equipment
{
	public class PlayerUI : NetworkBehaviour
	{
		private Dictionary<string, GameObject> ServerCache = new Dictionary<string, GameObject>();
		private string[] eventNames = new string[] {"suit", "belt", "feet", "head", "mask", "uniform", "neck", "ear", "eyes", "hands",
			"id", "back", "rightHand", "leftHand", "storage01", "storage02", "suitStorage"
		};

		public override void OnStartServer()
		{
			if (isServer) {
				foreach (string cacheName in eventNames) {
					ServerCache.Add(cacheName, null);
				}
			}
			base.OnStartServer();
		}

		//Server only
		public void TrySetItem(string eventName, GameObject obj)
		{
			if (ServerCache.ContainsKey(eventName)) {
				if (ServerCache[eventName] == null) {
					ServerCache[eventName] = obj;
					RpcTrySetItem(eventName, obj);
				}
			}
		}

		[ClientRpc]
		public void RpcTrySetItem(string eventName, GameObject obj)
		{
			if (isLocalPlayer) {
				if (eventName.Length > 0) {
					EventManager.UI.TriggerEvent(eventName, obj);
				}
			}
		}
	}
}