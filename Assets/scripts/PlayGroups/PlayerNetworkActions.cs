using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Events;
using PlayGroup;
using Equipment;
using Cupboards;
using UI;
using Items;
using System.Linq;

public partial class PlayerNetworkActions : NetworkBehaviour
{
    private Dictionary<string, GameObject> ServerCache = new Dictionary<string, GameObject>();
    private string[] eventNames = new string[]
    {"suit", "belt", "feet", "head", "mask", "uniform", "neck", "ear", "eyes", "hands",
        "id", "back", "rightHand", "leftHand", "storage01", "storage02", "suitStorage"
    };

    private Equipment.Equipment equipment;
    private PlayerMove playerMove;
    private PlayerSprites playerSprites;
    private PlayerScript playerScript;
    private SoundNetworkActions soundNetworkActions;
    private ChatIcon chatIcon;
    void Start()
    {
        equipment = GetComponent<Equipment.Equipment>();
        playerMove = GetComponent<PlayerMove>();
        playerSprites = GetComponent<PlayerSprites>();
        playerScript = GetComponent<PlayerScript>();
        soundNetworkActions = GetComponent<SoundNetworkActions>();
        chatIcon = GetComponentInChildren<ChatIcon>();
        CmdSyncRoundTime(GameManager.Instance.GetRoundTime);
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
    [Command]
    void CmdSyncRoundTime(float currentTime)
    {
        RpcSyncRoundTime(currentTime);
    }

    [ClientRpc]
    void RpcSyncRoundTime(float currentTime)
    {
        if (PlayerManager.LocalPlayer == gameObject)
        {
            GameManager.Instance.SyncTime(currentTime);
        }
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
                EquipmentPool.AddGameObject(gameObject, obj);
                ServerCache[eventName] = obj;
                equipment.SetHandItem(eventName, obj);
            }
            else
            {
                Debug.Log("ServerCache slot is full");
            }
        }
    }

    //This is for objects that aren't picked up via the hand (I.E a magazine clip inside a weapon that was picked up)
    [Command]
    public void CmdTryAddToEquipmentPool(GameObject obj)
    {

        EquipmentPool.AddGameObject(gameObject, obj);
    }

    [Command]
    public void CmdTryToInstantiateInHand(string eventName, GameObject prefab)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] == null)
            {
                GameObject item = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                NetworkServer.Spawn(item);
                EquipmentPool.AddGameObject(gameObject, item);
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
    void RpcInstantiateInHand(string playerName, GameObject item)
    {
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

    //Dropping from a slot on the UI
    [Command]
    public void CmdDropItem(string eventName)
    {
        if (ServerCache.ContainsKey(eventName))
        {
            if (ServerCache[eventName] != null)
            {
                GameObject item = ServerCache[eventName];
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
    //Dropping from somewhere else in the players equipmentpool (Magazine ejects from weapons etc)
    [Command]
    public void CmdDropItemNotInUISlot(GameObject obj)
    {
        EquipmentPool.DropGameObject(gameObject.name, obj);
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
                if (item != null && newParent != null)
                {
                    item.transform.parent = newParent.transform;
                    World.ReorderGameobjectsOnTile(pos);
                }
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
                if (att.spriteType == SpriteType.Clothing)
                {
                    // Debug.Log("eventName = " + eventName);
                    Epos enumA = (Epos)Enum.Parse(typeof(Epos), eventName);
                    equipment.syncEquipSprites[(int)enumA] = att.clothingReference;
                }
            }
        }
    }

    [Command]
    public void CmdStartMicrowave(GameObject microwave, string mealName)
    {
        Microwave m = microwave.GetComponent<Microwave>();
        m.ServerSetOutputMeal(mealName);
        m.RpcStartCooking();
    }

    [Command]
    public void CmdRequestJob(JobType jobType)
    {
        // Already have a job buddy!
        if (playerScript.JobType != JobType.NULL)
            return;

        playerScript.JobType = GameManager.Instance.GetRandomFreeOccupation(jobType);
        StartCoroutine(equipment.SetPlayerLoadOuts());
    }

    [Command]
    public void CmdToggleShutters(GameObject switchObj)
    {
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
    public void CmdToggleLightSwitch(GameObject switchObj)
    {
        Lighting.LightSwitchTrigger s = switchObj.GetComponent<Lighting.LightSwitchTrigger>();
        s.isOn = !s.isOn;
    }

    [Command]
    public void CmdToggleFireCabinet(GameObject cabObj, bool forItemInteract)
    {
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
    public void CmdMoveItem(GameObject item, Vector3 newPos)
    {
        item.transform.position = newPos;
    }

    [Command]
    public void CmdConsciousState(bool conscious)
    {
        if (conscious)
        {
            playerMove.allowInput = true;
            RpcSetPlayerRot(false, 0f);
        }
        else
        {
            playerMove.allowInput = false;
            RpcSetPlayerRot(false, -90f);
            soundNetworkActions.RpcPlayNetworkSound("Bodyfall", transform.position);
            if (UnityEngine.Random.value > 0.5f)
            {
                playerSprites.currentDirection = Vector2.up;
            }
        }
    }

    [Command]
    public void CmdSendChatMessage(string msg, bool isLocalChat)
    {
        if (isLocalChat)
        {
            //regex to sanitise any injected html tags
            var rx = new Regex("[<][^>]+[>]");
            var inputString = rx.Replace(msg, "");

            //might as well use it here so it doesn't matter how long the input string is
            rx = new Regex("^(/me )");
            if (rx.IsMatch(inputString))
            { // /me message
                inputString = rx.Replace(inputString, " ");
                ChatRelay.Instance.chatlog.Add(new ChatEvent("<i><b>" + gameObject.name + "</b>" + inputString + "</i>."));
            }
            else
            { // chat message
                ChatRelay.Instance.chatlog.Add(new ChatEvent("<b>" + gameObject.name + "</b>" + " says, " + "\"" + inputString + "\""));
            }
        }

    }


    [Command]
    //send a generic message
    public void CmdSendAlertMessage(string msg, bool isLocalChat)
    {
        if (isLocalChat)
        {
            ChatRelay.Instance.chatlog.Add(new ChatEvent(msg));
        }
    }

    [Command]
    public void CmdToggleChatIcon(bool turnOn)
    {
        RpcToggleChatIcon(turnOn);
    }

    [ClientRpc]
    void RpcToggleChatIcon(bool turnOn)
    {
        if (turnOn)
        {
            chatIcon.TurnOnTalkIcon();
        }
        else
        {
            chatIcon.TurnOffTalkIcon();
        }
    }

    //For falling over and getting back up again over network
    [ClientRpc]
    public void RpcSetPlayerRot(bool temporary, float rot)
    {
        var rotationVector = transform.rotation.eulerAngles;
        rotationVector.z = rot;
        transform.rotation = Quaternion.Euler(rotationVector);
        //So other players can walk over the Unconscious
        playerSprites.AdjustSpriteOrders(-30);
        if (temporary)
        {
            //TODO Coroutine with timer to get back up again
        }
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

    [ClientRpc]
    public void RpcSpawnGhost()
    {
        playerScript.ghost.SetActive(true);
        playerScript.ghost.transform.parent = null;
        chatIcon.gameObject.transform.parent = playerScript.ghost.transform;
        playerScript.ghost.transform.rotation = Quaternion.identity;
        if (PlayerManager.LocalPlayer == gameObject)
        {
            SoundManager.Stop("Critstate");
            Camera2DFollow.followControl.target = playerScript.ghost.transform;
            var fovScript = GetComponent<FieldOfView>();
            if (fovScript != null)
                fovScript.enabled = false;
        }
    }
}