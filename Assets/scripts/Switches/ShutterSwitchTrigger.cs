using InputControl;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShutterSwitchTrigger: InputTrigger
{

    public ShutterController[] shutters;

    [SyncVar(hook = "SyncShutters")]
    public bool IsClosed = false;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public override void Interact()
    {
        if (!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Switches_ShuttersUP"))
        {
            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleShutters(gameObject);
        }
    }

    void SyncShutters(bool isClosed)
    {
        if (isClosed)
        {
            OpenShutters();
        }
        else
        {
            CloseShutters();
       
        }
    }

    void OpenShutters()
    {
        foreach (var s in shutters)
        {
            s.Open();
        }
        animator.SetTrigger("activated");
    }

    void CloseShutters()
    {
        foreach (var s in shutters)
        {
            s.Close();
        }
        animator.SetTrigger("activated");
    }
}
