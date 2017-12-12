using InputControl;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using PlayGroups.Input;
using UnityEngine;
using UnityEngine.Networking;

public class ShutterSwitchTrigger : InputTrigger
{

    public ObjectTrigger[] TriggeringObjects;

    [SyncVar(hook = "SyncShutters")]
    public bool IsClosed;

    private Animator animator;

    void Start()
    {
        //This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
        gameObject.layer = LayerMask.NameToLayer("WallMounts");

        animator = GetComponent<Animator>();
    }

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLoad());
        base.OnStartClient();
    }

    IEnumerator WaitForLoad()
    {
        yield return new WaitForSeconds(3f);
        SyncShutters(IsClosed);
    }

    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        if (!PlayerManager.LocalPlayerScript.IsInReach(transform.position, 1.5f) || PlayerManager.LocalPlayerScript.playerMove.isGhost)
            return;

        //if the button is idle and not animating it can be pressed
        //this is weird it should check all children objects to see if they are idle and finished
        if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleShutters(gameObject);
        }
        else
        {
            Debug.Log("DOOR NOT FINISHED CLOSING YET!");
        }
    }

    void SyncShutters(bool isClosed)
    {
        foreach (var s in TriggeringObjects)
        {
            s.Trigger(isClosed);
        }
    }
}
