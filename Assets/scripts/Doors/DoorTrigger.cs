using InputControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger: InputTrigger {

    private DoorController doorController;

    public void Start() {
        doorController = GetComponent<DoorController>();
    }

    public override void Interact() {
        if(doorController.IsOpened)
            doorController.CmdTryClose();
        else
            doorController.CmdTryOpen(PlayGroup.PlayerManager.LocalPlayer);
    }
}
