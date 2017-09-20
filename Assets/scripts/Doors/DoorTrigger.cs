﻿using InputControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;

public class DoorTrigger: InputTrigger {

    private DoorController doorController;
    public bool allowInput = true;
    public void Start() {
        doorController = GetComponent<DoorController>();
    }

    public override void Interact(GameObject originator, string hand) {
        if (doorController != null && allowInput)
        {
            if ((int
                
                
                )doorController.restriction > 0)
            {
                if (UIManager.InventorySlots.IDSlot.IsFull && UIManager.InventorySlots.IDSlot.Item.GetComponent<ItemIdentity>() != null)
                {
                    CheckDoorAccess(UIManager.InventorySlots.IDSlot.Item.GetComponent<ItemIdentity>(), doorController);
                }else if (UIManager.Hands.CurrentSlot.IsFull && UIManager.Hands.CurrentSlot.Item.GetComponent<ItemIdentity>() != null)
                {
                    CheckDoorAccess(UIManager.Hands.CurrentSlot.Item.GetComponent<ItemIdentity>(), doorController);
                }
                else
                {
                    allowInput = false;
                    StartCoroutine(DoorInputCoolDown());
                    PlayGroup.PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRestrictDoorDenied(gameObject);
                }
            }
            else
            {
                allowInput = false;
                doorController.CmdTryOpen(gameObject);
                StartCoroutine(DoorInputCoolDown());
            }
        }
    }

    void CheckDoorAccess(ItemIdentity cardID, DoorController doorController){
        Debug.Log("been here!");
        if (cardID.Access.Contains(doorController.restriction))
        {// has access
            allowInput = false;
            PlayGroup.PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryOpenRestrictDoor(gameObject);
            Debug.Log(doorController.restriction.ToString() + " access granted");
            StartCoroutine(DoorInputCoolDown());
        }else
        {// does not have access
            Debug.Log(doorController.restriction.ToString() + " no access");
            allowInput = false;
            StartCoroutine(DoorInputCoolDown());
            PlayGroup.PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRestrictDoorDenied(gameObject);
        }
    }

    IEnumerator DoorInputCoolDown()
    {
        yield return new WaitForSeconds(0.3f);
        allowInput = true;
    }
}
