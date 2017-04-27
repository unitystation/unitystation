using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using PlayGroup;

namespace Items
{
	public class ItemNetwork: MonoBehaviour
	{
		void Start(){
			foreach (Transform child in transform) {
				ItemManager.Instance.itemsToSpawn.Add(child.gameObject);
			}
		}
	}
} 