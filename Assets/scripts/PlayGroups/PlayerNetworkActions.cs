using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Events;
using PlayGroup;
using Equipment;
using Cupboards;
using UI;

public class PlayerNetworkActions : NetworkBehaviour
{
    private Dictionary<string, GameObject> ServerCache = new Dictionary<string, GameObject>();
    private string[] eventNames = new string[]
    {"suit", "belt", "feet", "head", "mask", "uniform", "neck", "ear", "eyes", "hands",
        "id", "back", "rightHand", "leftHand", "storage01", "storage02", "suitStorage"
    };

    private Equipment.Equipment equipment;

    void Start()
    {
        equipment = GetComponent<Equipment.Equipment>();
    }

    public override void OnStartServer()
    {
        if (isServer)
        {
            foreach (string cacheName in eventNames)
            {
                ServerCache.Add(cacheName, null);
            }
        }
        base.OnStartServer();
    }
        
    //This is only called from interaction on the client (from PickUpTrigger)
    public bool TryToPickUpObject(GameObject itemObject)
    {            
        if (PlayerManager.PlayerScript != null)
        {
            if (!isLocalPlayer)
                return false;
				
            if (!UIManager.Hands.CurrentSlot.TrySetItem(itemObject))
            {
                return false;
            }
            else
            {
                CmdTryToPickUpObject(UIManager.Hands.CurrentSlot.eventName, itemObject);
            }
        }
        else
        {
            return false;
        }
        return true;
    }

    //Server only (from Equipment Initial SetItem method
    public void TrySetItem(string eventName, GameObject obj)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] == null)
            {
                ServerCache[eventName] = obj;
                RpcTrySetItem(eventName, obj);
            }
        }
    }

    [Command]
    public void CmdTryToPickUpObject(string eventName, GameObject obj)
    {			
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] == null || ServerCache[eventName] == obj)
            {
                EquipmentPool.AddGameObject(gameObject.name, obj);
                ServerCache[eventName] = obj;
                equipment.SetHandItem(eventName, obj);
            }
            else
            {
                Debug.Log("ServerCache slot is full");   
            }
        }
    }

    [Command]
    public void CmdTryToInstantiateInHand(string eventName, GameObject prefab){
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] == null)
            {
                GameObject item = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                NetworkServer.Spawn(item);
                EquipmentPool.AddGameObject(gameObject.name, item);
                ServerCache[eventName] = item;
                equipment.SetHandItem(eventName, item);
                RpcInstantiateInHand(gameObject.name, item);
            }
            else
            {
                Debug.Log("ServerCache slot is full");  
               
            }
        }
    }

    [ClientRpc]
    void RpcInstantiateInHand(string playerName, GameObject item){
        if (playerName == gameObject.name)
        {
            UIManager.Hands.CurrentSlot.TrySetItem(item);
        }
    }

    [ClientRpc]
    public void RpcTrySetItem(string eventName, GameObject obj)
    {
        if (isLocalPlayer)
        {
            if (eventName.Length > 0)
            {
                EventManager.UI.TriggerEvent(eventName, obj);
            }
        }
    }

    [Command]
    public void CmdDropItem(string eventName)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] != null)
            {
                EquipmentPool.DropGameObject(gameObject.name, ServerCache[eventName]);
                RpcAdjustItemParent(ServerCache[eventName], null);
                ServerCache[eventName] = null;
                equipment.ClearItemSprite(eventName);
            }
            else
            {
                Debug.Log("Object not found in ServerCache");
            }
        }
    }

    [Command]
    public void CmdPlaceItem(string eventName, Vector3 pos, GameObject newParent)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] != null)
            {
                GameObject item = ServerCache[eventName];
                EquipmentPool.DropGameObject(gameObject.name, ServerCache[eventName], pos);
                ServerCache[eventName] = null;
                item.transform.parent = newParent.transform;
                RpcAdjustItemParent(item, newParent);
                equipment.ClearItemSprite(eventName);
            }
        }
    }

    [Command]
    public void CmdPlaceItemCupB(string eventName, Vector3 pos, GameObject newParent)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] != null)
            {
                GameObject item = ServerCache[eventName];
                EquipmentPool.DropGameObject(gameObject.name, ServerCache[eventName], pos);
                ServerCache[eventName] = null;
                ClosetControl closetCtrl = newParent.GetComponent<ClosetControl>();
                item.transform.parent = closetCtrl.items.transform;
                RpcAdjustItemParentCupB(item, newParent);
                equipment.ClearItemSprite(eventName);
            }
        }
    }

    [Command]
    public void CmdToggleCupboard(GameObject cupbObj)
    {
        ClosetControl closetControl = cupbObj.GetComponent<ClosetControl>();
        closetControl.ServerToggleCupboard();
    }

    [Command]
    public void CmdClearUISlot(string eventName)
    {
        ServerCache[eventName] = null;

        if (eventName == "id" || eventName == "storage01" || eventName == "storage02" || eventName == "suitStorage")
        {
        }
        else
        {
            equipment.ClearItemSprite(eventName);
        }
    }
     
    [Command]
    public void CmdSetUISlot(string eventName, GameObject obj)
    {
        ServerCache[eventName] = obj;
        ItemAttributes att = obj.GetComponent<ItemAttributes>();
        if (eventName == "leftHand" || eventName == "rightHand")
        {
            equipment.SetHandItemSprite(eventName, att);
        }
        else
        {
            if (eventName == "id" || eventName == "storage01" || eventName == "storage02" || eventName == "suitStorage")
            {
            }
            else
            {
                if (att.spriteType == UI.SpriteType.Clothing) {
                    Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
                    equipment.syncEquipSprites[(int)enumA] = att.clothingReference;
                }
            }
        }
    }

    [Command]
    public void CmdStartMicrowave(GameObject microwave, string mealName){
        Microwave m = microwave.GetComponent<Microwave>();
        m.ServerSetOutputMeal(mealName);
        m.RpcStartCooking();
    }

    [Command]
    public void CmdAttackMob(GameObject npcObj)
    {
        Living attackTarget = npcObj.GetComponent<Living>();
        attackTarget.RpcReceiveDamage();
    }

    [Command]
    public void CmdHarvestMob(GameObject npcObj)
    {
        Living attackTarget = npcObj.GetComponent<Living>();
        attackTarget.RpcHarvest();
    }

    [Command]
    public void CmdToggleShutters(GameObject switchObj){
        ShutterSwitchTrigger s = switchObj.GetComponent<ShutterSwitchTrigger>();
        if (s.IsClosed)
        {
            s.IsClosed = false;
        }
        else
        {
            s.IsClosed = true;
        }
    }

    [Command]
    public void CmdToggleLightSwitch(GameObject switchObj){
        Lighting.LightSwitchTrigger s = switchObj.GetComponent<Lighting.LightSwitchTrigger>();
        if (s.isOn)
        {
            s.isOn = false;
        }
        else
        {
            s.isOn = true;
        }
    }

    [Command]
    public void CmdToggleFireCabinet(GameObject cabObj, bool forItemInteract){
        CabinetTrigger c = cabObj.GetComponent<CabinetTrigger>();

        if (!forItemInteract)
        {
            if (c.IsClosed)
            {
                c.IsClosed = false;
            }
            else
            {
                c.IsClosed = true;
            }
        }
        else
        {
            Debug.Log("TODO: condition to place extinguisher back");
            c.RpcSetEmptySprite();
        }
    }
        
    [Command]
    public void CmdShootBullet(Vector2 direction, string bulletName){
        GameObject bullet = GameObject.Instantiate(Resources.Load(bulletName) as GameObject,transform.position, Quaternion.identity);
        NetworkServer.Spawn(bullet);
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        BulletBehaviour b = bullet.GetComponent<BulletBehaviour>();
        b.Shoot(direction, angle);
            
    }

    [ClientRpc]
    void RpcAdjustItemParent(GameObject item, GameObject parent)
    {
        if (parent != null)
        {
            item.transform.parent = parent.transform;
        }
        else
        {
            item.transform.parent = null;
        }
    }

    [ClientRpc]
    void RpcAdjustItemParentCupB(GameObject item, GameObject parent)
    {
        if (parent != null)
        {
            ClosetControl closetCtrl = parent.GetComponent<ClosetControl>();
            item.transform.parent = closetCtrl.items.transform;
        }
        else
        {
            item.transform.parent = null;
        }
    }
}
