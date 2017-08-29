using InputControl;
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
            if (doorController.restriction.Length > 0)
            {
                if (UIManager.InventorySlots.IDSlot.IsFull && UIManager.InventorySlots.IDSlot.Item.GetComponent<ItemIdentity>() != null)
                {
                    if (UIManager.InventorySlots.IDSlot.Item.GetComponent<ItemIdentity>().Access.Contains(doorController.restriction))
                    {
                        allowInput = false;
                        doorController.CmdTryOpen(gameObject);
                        StartCoroutine(DoorInputCoolDown());
                    }
                    else
                    {
                        allowInput = false;
                        StartCoroutine(DoorInputCoolDown());
                    }
                }
                else
                {
                    allowInput = false;
                    StartCoroutine(DoorInputCoolDown());
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
    IEnumerator DoorInputCoolDown()
    {
        yield return new WaitForSeconds(0.3f);
        allowInput = true;
    }
}
