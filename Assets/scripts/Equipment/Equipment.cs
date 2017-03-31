using Events;
using System;
using PlayGroup;
using Sprites;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;

namespace Equipment
{
	public class Equipment: NetworkBehaviour
	{
		public GameObject suitPrefab;
		public GameObject beltPrefab;
		public GameObject shoesPrefab;
		public GameObject headPrefab;
		public GameObject maskPrefab;
		public GameObject uniformPrefab;
		public GameObject neckPrefab;
		public GameObject earPrefab;
		public GameObject glassesPrefab;
		public GameObject glovesPrefab;

		public GameObject leftHandPrefab;
		public GameObject rightHandPrefab;

		public GameObject idPrefab;
		public GameObject bagPrefab;
		public GameObject storage01Prefab;
		public GameObject storage02Prefab;
		public GameObject suitStoragePrefab;

		public SyncListInt syncEquip = new SyncListInt();

		private Dictionary<string, UI_ItemSlot> uiSlots = new Dictionary<string, UI_ItemSlot>();
		private Dictionary<string,ClothingItem> clothingSlots = new Dictionary<string,ClothingItem>();

		private bool isInit = false;

		void Start()
		{
			syncEquip.Callback = SyncEquipment;
		}

		public override void OnStartServer()
		{
			InitEquipment();
		
			EquipmentPool equipPool = FindObjectOfType<EquipmentPool>();
			if (equipPool == null) {
				Instantiate(Resources.Load("EquipmentPool") as GameObject, Vector2.zero, Quaternion.identity);
			}

			StartCoroutine(SetPlayerLoadOuts());
			
			base.OnStartServer();
		}

		public override void OnStartClient()
		{
			InitEquipment();
			base.OnStartClient();
		}

		void InitEquipment()
		{
			if (isInit)
				return;
		
//			foreach (var itemSlot in UIManager.Instance.GetComponentsInChildren(typeof(UI_ItemSlot), true)) {
//				var name = itemSlot.transform.parent.name;
//				var slot = (UI_ItemSlot)itemSlot;
//				if (!uiSlots.ContainsKey(name)) {
//					uiSlots.Add(name, slot);
//				}
//			}

			foreach (Transform child in transform) {
				ClothingItem c = child.gameObject.GetComponent<ClothingItem>();
				if (c != null && !clothingSlots.ContainsKey(c.gameObject.name)) {
					clothingSlots.Add(c.gameObject.name, c);
					if (c.gameObject.name == "body") {
						
						if (isServer) {
							c.Reference = 32;
							syncEquip.Add(32);
						} else {
							Epos enumA = (Epos)Enum.Parse(typeof(Epos), c.gameObject.name);
							c.Reference = syncEquip[(int)enumA];
						}
					} else {
						c.Reference = -1;
						if (isServer) {
							syncEquip.Add(-1);
						} else {
							Epos enumA = (Epos)Enum.Parse(typeof(Epos), c.gameObject.name);
							c.Reference = syncEquip[(int)enumA];
						}
					}
				}
			}
			isInit = true;
				
		}

		public void SyncEquipment(SyncListInt.Operation op, int index)
		{
			Epos enumA = (Epos)index;
			string key = enumA.ToString();
			clothingSlots[key].Reference = syncEquip[index];
		}

		IEnumerator SetPlayerLoadOuts()
		{
			//Waiting for player name resolve
			yield return new WaitForSeconds(0.2f);

			SetItem("suit", suitPrefab);
			SetItem("belt", beltPrefab);
			SetItem("feet", shoesPrefab);
			SetItem("head", headPrefab);
			SetItem("mask", maskPrefab);
			SetItem("uniform", uniformPrefab);
			SetItem("neck", neckPrefab);
			SetItem("ear", earPrefab);
			SetItem("eyes", glassesPrefab);
			SetItem("hands", glovesPrefab);
//
//		SetItem("id", idPrefab);
//		SetItem("back", bagPrefab);
//		SetItem("rightHand", rightHandPrefab);
//		SetItem("leftHand", leftHandPrefab);
//		SetItem("storage01", storage01Prefab);
//		SetItem("storage02", storage02Prefab);
//		SetItem("suitStorage", suitStoragePrefab);
		}

		private void SetItem(string eventName, GameObject prefab)
		{
			if (prefab == null)
				return;
			
			GameObject item = Instantiate(prefab, Vector2.zero, Quaternion.identity) as GameObject;
			NetworkServer.Spawn(item);
			ItemAttributes att = item.GetComponent<ItemAttributes>();
			EquipmentPool.AddGameObject(gameObject.name, item);

			//Sync all clothing items across network using SyncListInt syncEquip
			if (att.spriteType == UI.SpriteType.Clothing) {
				Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
				syncEquip[(int)enumA] = att.clothingReference;
			}

			//TODO UI SYNC: SET ITEMS UP FOR CSERVER --> CLIENT UI INFORMATION UPDATE
//			if (prefab != null && isLocalPlayer) {
//				if (eventName.Length > 0)
//					EventManager.UI.TriggerEvent(eventName, prefab);
//			}
		}
			
	}
}