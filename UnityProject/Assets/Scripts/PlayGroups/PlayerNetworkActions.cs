﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cupboards;
using Equipment;
using InputControl;
using Lighting;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;
using System.Linq;
using Doors;
using PlayGroups.Input;
using Tilemaps.Scripts.Behaviours.Objects;

public partial class PlayerNetworkActions : NetworkBehaviour
{
    private Dictionary<string, GameObject> _inventory = new Dictionary<string, GameObject>();

    private string[] slotNames =
    {
        "suit", "belt", "feet", "head", "mask", "uniform", "neck", "ear", "eyes", "hands",
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
    }

    public override void OnStartServer()
    {
        if (isServer)
        {
            foreach (string slotName in slotNames)
            {
                _inventory.Add(slotName, null);
            }
        }
        else
        {
            CmdSyncRoundTime(GameManager.Instance.GetRoundTime);
        }
        base.OnStartServer();
    }

    public Dictionary<string, GameObject> Inventory
    {
        get { return _inventory; }
    }

    [Server]
    public bool AddItem(GameObject itemObject, string slotName = null, bool replaceIfOccupied = false, bool forceInform = true)
    {
        var eventName = slotName ?? UIManager.Hands.CurrentSlot.eventName;
        if (_inventory[eventName] != null && _inventory[eventName] != itemObject && !replaceIfOccupied)
        {
            Debug.LogFormat("{0}: Didn't replace existing {1} item {2} with {3}",
                gameObject.name, eventName, _inventory[eventName].name, itemObject.name);
            return false;
        }
        EquipmentPool.AddGameObject(gameObject, itemObject);
        SetInventorySlot(slotName, itemObject);
        UpdateSlotMessage.Send(gameObject, eventName, itemObject, forceInform);
        return true;
    }

    [Server]
    public void PlaceInSlot(GameObject item, string slotName)
    {
        UIManager.InventorySlots.GetSlotByEvent(slotName).SetItem(item);
    }

    void PlaceInHand(GameObject item)
    {
        UIManager.Hands.CurrentSlot.SetItem(item);
    }

    //This is for objects that aren't picked up via the hand (I.E a magazine clip inside a weapon that was picked up)
    //TODO make these private(make some public child-aware high level methods instead):
    [Server]
    public void RemoveFromEquipmentPool(GameObject obj)
    {
        EquipmentPool.DropGameObject(gameObject, obj);
    }

    [Server]
    public void AddToEquipmentPool(GameObject obj)
    {
        EquipmentPool.AddGameObject(gameObject, obj);
    }

    [Server]
    public bool ValidateInvInteraction(string slot, GameObject gObj = null, bool forceClientInform = true)
    {
        if (!_inventory[slot] && gObj && _inventory.ContainsValue(gObj))
        {
            UpdateSlotMessage.Send(gameObject, slot, gObj, forceClientInform);
            SetInventorySlot(slot, gObj);
            //Clean up other slots
            ClearObjectIfNotInSlot(gObj, slot, forceClientInform);
            //Debug.LogFormat("Approved moving {0} to slot {1}", gObj, slot);
            return true;
        }
        if (!gObj)
        {
            return ValidateDropItem(slot, forceClientInform);
        }
        Debug.LogWarningFormat("Unable to validateInvInteraction {0}:{1}", slot, gObj.name);
        return false;
    }

    public void RollbackPrediction(string slot)
    {
        UpdateSlotMessage.Send(gameObject, slot, _inventory[slot], true);
    }

    [Server]
    private void ClearObjectIfNotInSlot(GameObject gObj, string slot, bool forceClientInform)
    {
        HashSet<string> toBeCleared = new HashSet<string>();
        foreach (string key in _inventory.Keys)
        {
            if (key.Equals(slot) || !_inventory[key]) continue;
            if (_inventory[key].Equals(gObj))
            {
                toBeCleared.Add(key);
            }
        }
        ClearInventorySlot(forceClientInform, toBeCleared.ToArray());
    }

    [Server]
    public void ClearInventorySlot(params string[] slotNames)
    {
        ClearInventorySlot(true, slotNames);
    }

    [Server]
    private void ClearInventorySlot(bool forceClientInform, params string[] slotNames)
    {
        for (int i = 0; i < slotNames.Length; i++)
        {
            _inventory[slotNames[i]] = null;
            if (slotNames[i] == "id" || slotNames[i] == "storage01"
                || slotNames[i] == "storage02" || slotNames[i] == "suitStorage")
            {
                //Not clearing onPlayer sprites for these as they don't have any
            }
            else
            {
                equipment.ClearItemSprite(slotNames[i]);
            }
            UpdateSlotMessage.Send(gameObject, slotNames[i], null, forceClientInform);
        }
        //        Debug.LogFormat("Cleared {0}", slotNames);
    }

    [Server]
    public void SetInventorySlot(string slotName, GameObject obj)
    {
        _inventory[slotName] = obj;
        ItemAttributes att = obj.GetComponent<ItemAttributes>();
        if (slotName == "leftHand" || slotName == "rightHand")
        {
            equipment.SetHandItemSprite(slotName, att);
        }
        else
        {
            if (slotName == "id" || slotName == "storage01"
                || slotName == "storage02" || slotName == "suitStorage")
            {
                //Not setting onPlayer sprites for these as they don't have any
            }
            else
            {
                if (att.spriteType == SpriteType.Clothing || att.hierarchy.Contains("headset"))
                {
                    // Debug.Log("slotName = " + slotName);
                    Epos enumA = (Epos) Enum.Parse(typeof(Epos), slotName);
                    equipment.syncEquipSprites[(int) enumA] = att.clothingReference;
                }
            }
        }
    }

    /// Drop an item from a slot. use forceSlotUpdate=false when doing clientside prediction, 
    /// otherwise client will forcefully receive update slot messages
    public void DropItem(string hand, bool forceClientInform = true)
    {
        if (CustomNetworkManager.Instance._isServer)
            ValidateDropItem(hand, forceClientInform);
        else
            InventoryInteractMessage.Send(hand, null, forceClientInform);
    }

    //Dropping from a slot on the UI
    [Server]
    public bool ValidateDropItem(string slot, bool forceClientInform /* = false*/)
    {
        //decline if not dropped from hands?
        if (_inventory.ContainsKey(slot) && _inventory[slot])
        {
            EquipmentPool.DropGameObject(gameObject, _inventory[slot]);

            //            RpcAdjustItemParent(_inventory[slot], null);
            _inventory[slot] = null;
            equipment.ClearItemSprite(slot);
            UpdateSlotMessage.Send(gameObject, slot, null, forceClientInform);
            return true;
        }
        Debug.Log("Object not found in Inventory for: " + gameObject.name);
        return false;
    }

    //Dropping from somewhere else in the players equipmentpool (Magazine ejects from weapons etc)
    [Command]
    [Obsolete]
    public void CmdDropItemNotInUISlot(GameObject obj)
    {
        EquipmentPool.DropGameObject(gameObject, obj);
    }

    public void DisposeOfChildItem(GameObject obj)
    {
        EquipmentPool.DisposeOfObject(gameObject, obj);
    }

    [Server]
    public void PlaceItem(string slotName, Vector3 pos, GameObject newParent)
    {
        if (!SlotNotEmpty(slotName)) return;
        GameObject item = _inventory[slotName];
        EquipmentPool.DropGameObject(gameObject, _inventory[slotName], pos);
        ClearInventorySlot(slotName);
        if (item != null && newParent != null)
        {
            item.transform.parent = newParent.transform;
            // TODO reorder items
            //			World.ReorderGameobjectsOnTile(pos);
        }
    }

    public bool SlotNotEmpty(string eventName)
    {
        return _inventory.ContainsKey(eventName) && _inventory[eventName] != null;
    }

    [Command]
    public void CmdToggleCupboard(GameObject cupbObj)
    {
        ClosetControl closetControl = cupbObj.GetComponent<ClosetControl>();
        closetControl.ServerToggleCupboard();
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

        RespawnPlayer();
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
        LightSwitchTrigger s = switchObj.GetComponent<LightSwitchTrigger>();
        s.isOn = !s.isOn;
    }

    [Command]
    public void CmdToggleFireCabinet(GameObject cabObj, bool forItemInteract,
        string currentSlotName)
    {
        FireCabinetTrigger c = cabObj.GetComponent<FireCabinetTrigger>();

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
            if (c.isFull)
            {
                c.isFull = false;
                if (AddItem(c.storedObject.gameObject, currentSlotName))
                {
                    c.storedObject.visibleState = true;
                    c.storedObject = null;
                }
            }
            else
            {
                c.storedObject = _inventory[currentSlotName].GetComponent<ObjectBehaviour>();
                ClearInventorySlot(currentSlotName);
                c.storedObject.visibleState = false;
                c.isFull = true;
            }
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
            if (Random.value > 0.5f)
            {
                playerSprites.currentDirection = Vector2.up;
            }
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
        //		Debug.LogWarning("Setting TileType to none for player and adjusting sortlayers in RpcSetPlayerRot");
        SpriteRenderer[] spriteRends = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sR in spriteRends)
        {
            sR.sortingLayerName = "Blood";
        }
        gameObject.GetComponent<RegisterPlayer>().IsBlocking = false;
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
            {
                fovScript.enabled = false;
            }
            //Show ghosts and hide FieldOfView
            Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("Ghosts"));
            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("FieldOfView"));
        }
    }


    //Respawn action for Deathmatch v 0.1.3

    [Server]
    public void RespawnPlayer(int timeout = 0)
    {
        StartCoroutine(InitiateRespawn(timeout));
    }

    [Server]
    private IEnumerator InitiateRespawn(int timeout)
    {
        Debug.LogFormat("{0}: Initiated respawn in {1}s", gameObject.name, timeout);
        yield return new WaitForSeconds(timeout);
        RpcAdjustForRespawn();

        EquipmentPool.ClearPool(gameObject.name);

        PlayerList.Instance.RemovePlayer(gameObject.name);

        SpawnHandler.RespawnPlayer(connectionToClient, playerControllerId, playerScript.JobType);
    }

    [ClientRpc]
    private void RpcAdjustForRespawn()
    {
        playerScript.ghost.SetActive(false);
        //Hide ghosts and show FieldOfView
        Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("Ghosts"));
        Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("FieldOfView"));
        gameObject.GetComponent<InputController>().enabled = false;
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

    [Command]
    public void CmdTryOpenDoor(GameObject door)
    {
        door.GetComponent<DoorController>().CmdTryOpen(gameObject);
    }

    [Command]
    public void CmdTryCloseDoor(GameObject door)
    {
        door.GetComponent<DoorController>().CmdTryClose();
    }

    [Command]
    public void CmdRestrictDoorDenied(GameObject door)
    {
        door.GetComponent<DoorController>().CmdTryDenied();
    }
}
