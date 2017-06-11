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

		public SyncListInt syncEquipSprites = new SyncListInt();
		public ClothingItem[] clothingSlots;
        private PlayerNetworkActions playerNetworkActions;
		public NetworkIdentity networkIdentity{ get; set; }

		private bool isInit = false;

		void Start()
		{
			networkIdentity = GetComponent<NetworkIdentity>();
            playerNetworkActions = gameObject.GetComponent<PlayerNetworkActions>();

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

			syncEquipSprites.Callback = SyncSprites;
			for (int i = 0; i < clothingSlots.Length ; i++) {
					//All the other slots:
					clothingSlots[i].Reference = -1;
						if (isServer) {
							syncEquipSprites.Add(-1);
						} else {
						clothingSlots[i].Reference = syncEquipSprites[i];
						}
					}
			isInit = true;
			//Player sprite offset:
			clothingSlots[10].Reference = 33;
				
		}

		public void SyncSprites(SyncListInt.Operation op, int index)
		{
			clothingSlots[index].Reference = syncEquipSprites[index];
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

			SetItem("id", idPrefab);
			SetItem("back", bagPrefab);
			SetItem("rightHand", rightHandPrefab);
			SetItem("leftHand", leftHandPrefab);
			SetItem("storage01", storage01Prefab);
			SetItem("storage02", storage02Prefab);
			SetItem("suitStorage", suitStoragePrefab);
		}

		//Hand item sprites after picking up an item (server)
		public void SetHandItem(string eventName, GameObject obj)
		{
			ItemAttributes att = obj.GetComponent<ItemAttributes>();
			EquipmentPool.AddGameObject(gameObject, obj);
            SetHandItemSprite(eventName, att);
            RpcSendMessage(eventName, obj);
		}

        [ClientRpc]
        void RpcSendMessage(string eventName, GameObject obj){
            obj.BroadcastMessage("OnAddToInventory",eventName, SendMessageOptions.DontRequireReceiver);
        }

        //To set the actual sprite on the player obj
        public void SetHandItemSprite(string eventName, ItemAttributes att){
            Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
            if (eventName == "leftHand") {
                syncEquipSprites[(int)enumA] = att.NetworkInHandRefLeft();
            } else {
                syncEquipSprites[(int)enumA] = att.NetworkInHandRefRight();
            }
        }
          
		//Clear any sprite slot with -1 via the eventName (server)
		public void ClearItemSprite(string eventName){
			Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
			syncEquipSprites[(int)enumA] = -1;
		}

		private void SetItem(string eventName, GameObject prefab)
		{
			if (prefab == null)
				return;
			
			GameObject item = Instantiate(prefab, Vector2.zero, Quaternion.identity) as GameObject;
			NetworkServer.Spawn(item);
			ItemAttributes att = item.GetComponent<ItemAttributes>();
			EquipmentPool.AddGameObject(gameObject, item);

			playerNetworkActions.TrySetItem(eventName, item);
			//Sync all clothing items across network using SyncListInt syncEquipSprites
			if (att.spriteType == UI.SpriteType.Clothing) {
				Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
				syncEquipSprites[(int)enumA] = att.clothingReference;
			}
		}		
	}
}