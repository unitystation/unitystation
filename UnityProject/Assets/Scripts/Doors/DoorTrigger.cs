using InputControl;
using System.Collections;
using System.Collections.Generic;
using PlayGroups.Input;
using UnityEngine;
using UI;

namespace Doors
{

    /// <summary>
    /// Handles Interact messages from InputController.cs 
    /// It also checks for access restrictions on the players ID card
    /// </summary>
    public class DoorTrigger : InputTrigger
    {
        private DoorController doorController;
        public bool allowInput = true;
        public void Start()
        {
            doorController = GetComponent<DoorController>();
        }

        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
            if (doorController != null && allowInput)
            {
                if ((int)doorController.restriction > 0)
                {
                    if (UIManager.InventorySlots.IDSlot.IsFull && UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>() != null)
                    {
                        CheckDoorAccess(UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>(), doorController);
                    }
                    else if (UIManager.Hands.CurrentSlot.IsFull && UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>() != null)
                    {
                        CheckDoorAccess(UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>(), doorController);
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
                    if (CustomNetworkManager.Instance._isServer)
                    {
                        if (!doorController.IsOpened)
                            doorController.CmdTryOpen(gameObject);
                        else
                            doorController.CmdTryClose();
                    }
                    else
                    {
                        //for mouse click opening when not server
                        if (!doorController.IsOpened)
                            PlayGroup.PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryOpenDoor(gameObject);
                        else
                            PlayGroup.PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryCloseDoor(gameObject);

                    }
                    StartCoroutine(DoorInputCoolDown());
                }
            }
        }

        void CheckDoorAccess(IDCard cardID, DoorController doorController)
        {
            Debug.Log("been here!");
            if (cardID.accessSyncList.Contains((int)doorController.restriction))
            {// has access
                allowInput = false;
                if (!doorController.IsOpened)
                    PlayGroup.PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryOpenDoor(gameObject);
                else
                    PlayGroup.PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryCloseDoor(gameObject);

                Debug.Log(doorController.restriction.ToString() + " access granted");
                StartCoroutine(DoorInputCoolDown());
            }
            else
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
}
