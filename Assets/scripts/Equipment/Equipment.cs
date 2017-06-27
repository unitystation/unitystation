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
    public class Equipment : NetworkBehaviour
    {
        public SyncListInt syncEquipSprites = new SyncListInt();
        public ClothingItem[] clothingSlots;
        private PlayerNetworkActions playerNetworkActions;

        public NetworkIdentity networkIdentity { get; set; }

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
            if (equipPool == null)
            {
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
            for (int i = 0; i < clothingSlots.Length; i++)
            {
                //All the other slots:
                clothingSlots[i].Reference = -1;
                if (isServer)
                {
                    syncEquipSprites.Add(-1);
                }
                else
                {
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

            JobType jobType = GameManager.Instance.GetRandomFreeOccupation();

            PlayerScript pS = GetComponent<PlayerScript>();
            pS.JobType = jobType;

            foreach (string startingItemHierPath in GameManager.Instance.GetOccupationEquipment(jobType))
            {
                GameObject obj = ClothFactory.CreateCloth(startingItemHierPath, Vector3.zero);
                ItemAttributes itemAtts = obj.GetComponent<ItemAttributes>();
                SetItem(GetLoadOutEventName(itemAtts.type), itemAtts);
            }
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
        void RpcSendMessage(string eventName, GameObject obj)
        {
            obj.BroadcastMessage("OnAddToInventory", eventName, SendMessageOptions.DontRequireReceiver);
        }

        public string GetLoadOutEventName(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Glasses:
                    return "eyes";
                case ItemType.Hat:
                    return "head";
                case ItemType.Neck:
                    return "neck";
                case ItemType.Mask:
                    return "mask";
                case ItemType.Ear:
                    return "ear";
                case ItemType.Suit:
                    return "suit";
                case ItemType.Uniform:
                    return "uniform";
                case ItemType.Gloves:
                    return "hands";
                case ItemType.Shoes:
                    return "feet";
                case ItemType.Belt:
                    return "belt";
                case ItemType.Back:
                    return "back";
                case ItemType.ID:
                    return "id";
                case ItemType.PDA:
                    return "storage02";
                case ItemType.Food:
                    return "storage01";
                case ItemType.Knife:
                    return "leftHand";
                case ItemType.Gun:
                    return "rightHand";
                default:
                    Debug.LogWarning("GetLoadOutEventName: Unknown ItemType:" + itemType.ToString());
                    return null;
            }
        }

        //To set the actual sprite on the player obj
        public void SetHandItemSprite(string eventName, ItemAttributes att)
        {
            Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
            if (eventName == "leftHand")
            {
                syncEquipSprites[(int)enumA] = att.NetworkInHandRefLeft();
            }
            else
            {
                syncEquipSprites[(int)enumA] = att.NetworkInHandRefRight();
            }
        }

        //Clear any sprite slot with -1 via the eventName (server)
        public void ClearItemSprite(string eventName)
        {
            Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
            syncEquipSprites[(int)enumA] = -1;
        }

        // Does not try to instantiate (already instantiated by Unicloth factory)
        private void SetItemUniclothToSlot(string eventName, GameObject uniCloth)
        {
            ItemAttributes att = uniCloth.GetComponent<ItemAttributes>();
            EquipmentPool.AddGameObject(gameObject, uniCloth);

            playerNetworkActions.TrySetItem(eventName, uniCloth);
            //Sync all clothing items across network using SyncListInt syncEquipSprites
            if (att.spriteType == SpriteType.Clothing)
            {
                Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
                syncEquipSprites[(int)enumA] = att.clothingReference;
            }
        }

        private void SetItem(string eventName, ItemAttributes itemAtts)
        {
            if (String.IsNullOrEmpty(eventName) || itemAtts == null)
                return;

            EquipmentPool.AddGameObject(gameObject, itemAtts.gameObject);

            playerNetworkActions.TrySetItem(eventName, itemAtts.gameObject);
            //Sync all clothing items across network using SyncListInt syncEquipSprites
            if (itemAtts.spriteType == SpriteType.Clothing)
            {
                Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
                syncEquipSprites[(int)enumA] = itemAtts.clothingReference;
            }
        }
    }
}