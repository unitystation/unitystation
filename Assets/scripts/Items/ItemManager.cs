using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using UI;

namespace Items {

	public class ItemManager: MonoBehaviour {
		public List<GameObject> itemsToSpawn = new List<GameObject>();
		private static ItemManager itemManager;
		public static ItemManager Instance {
			get {
				if(!itemManager) {
					itemManager = FindObjectOfType<ItemManager>();
				}
				return itemManager;
			}
		}

		public static void DestroyItemsInSpawnList(){
			foreach (GameObject obj in Instance.itemsToSpawn) {
				Destroy(obj);
			}
		}
	}
}