using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;

namespace Doors
{
    /// <summary>
    ///     Handles Interact messages from InputController.cs
    ///     It also checks for access restrictions on the players ID card
    /// </summary>
    public class DoorTrigger : InputTrigger
    {
        public bool allowInput = true;
        private DoorController doorController;

        public void Start()
        {
            doorController = GetComponent<DoorController>();
        }

        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
            if (doorController != null && allowInput)
            {
                if ((int) doorController.restriction > 0)
                {
                    if (UIManager.InventorySlots.IDSlot.IsFull &&
                        UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>() != null)
                    {
                        CheckDoorAccess(UIManager.InventorySlots.IDSlot.Item.GetComponent<IDCard>(), doorController, originator);
                    }
                    else if (UIManager.Hands.CurrentSlot.IsFull &&
                             UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>() != null)
                    {
                        CheckDoorAccess(UIManager.Hands.CurrentSlot.Item.GetComponent<IDCard>(), doorController, originator);
                    }
                    else
                    {
                        allowInput = false;
                        StartCoroutine(DoorInputCoolDown());
                        PlayerManager.LocalPlayerScript.playerNetworkActions
                            .CmdRestrictDoorDenied(gameObject);
                    }
                }
                else
                {
                    Debug.Log("no restriction required");
                    allowInput = false;
                    if (CustomNetworkManager.Instance._isServer)
                    {
                        if (!doorController.IsOpened)
                        {
                            doorController.CmdTryOpen(originator);
                        }
                        else
                        {
                            doorController.CmdTryClose();
                        }
                    }
                    else
                    {
                        //for mouse click opening when not server
                        if (!doorController.IsOpened)
                        {
                            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryOpenDoor(gameObject, originator);
                        }
                        else
                        {
                            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryCloseDoor(gameObject);
                        }
                    }
                    StartCoroutine(DoorInputCoolDown());
                }
            }
        }

        private void CheckDoorAccess(IDCard cardID, DoorController doorController, GameObject originator)
        {
            Debug.Log("been here!");
            if (cardID.accessSyncList.Contains((int) doorController.restriction))
            {
// has access
                allowInput = false;
                if (!doorController.IsOpened)
                {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryOpenDoor(gameObject, originator);
                }
                else
                {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryCloseDoor(gameObject);
                }

                Debug.Log(doorController.restriction + " access granted");
                StartCoroutine(DoorInputCoolDown());
            }
            else
            {
// does not have access
                Debug.Log(doorController.restriction + " no access");
                allowInput = false;
                StartCoroutine(DoorInputCoolDown());
                PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRestrictDoorDenied(gameObject);
            }
        }

        private IEnumerator DoorInputCoolDown()
        {
            yield return new WaitForSeconds(0.3f);
            allowInput = true;
        }
    }
}