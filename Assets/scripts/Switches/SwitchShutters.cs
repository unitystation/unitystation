using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchShutters : Photon.PunBehaviour
{

    public ShutterController[] shutters;

    private Animator animator;
    private bool closed = false;
    public PhotonView photonView;
    private bool synced = false;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnMouseDown()
    {
        if (PlayerManager.PlayerScript != null)
        {
            if (PlayerManager.PlayerScript.DistanceTo(transform.position) <= 2f)
            {
                if (!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Switches_ShuttersUP"))
                {
                    if (closed)
                    {
                        if (PhotonNetwork.connectedAndReady) //Send open to all other clients
                        {
                            photonView.RPC("OpenShutters", PhotonTargets.All, null);
                        }
                        else
                        {
                            OpenShutters(); //Dev mode 
                        }
                    }
                    else
                    {
                        
                        if (PhotonNetwork.connectedAndReady) //Send close to all other clients
                        {
                            photonView.RPC("CloseShutters", PhotonTargets.All, null);
                        }
                        else
                        {
                            CloseShutters(); //dev mode
                        }
                    }

                    closed = !closed;
                    animator.SetTrigger("activated");
                }
            }
        }
    }

    [PunRPC]
    private void OpenShutters()
    {
        foreach (var s in shutters)
        {
            s.Open();
        }
    }

    [PunRPC]
    private void CloseShutters()
    {
        foreach (var s in shutters)
        {
            s.Close();
        }
    }

    //Pun Sync
    [PunRPC]
    void SendCurrentState() //Master client must update all other clients on join on shutter state
    {
        photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, closed);
    }

    [PunRPC]
    void ReceiveCurrentState(bool closedState)
    {
        if (closed != closedState)
        {
            if (closedState)
            {
                CloseShutters();
            }
            else
            {
                OpenShutters();
            }
        }
    }

    public override void OnJoinedRoom()
    {
        //Update on join if this item was not instantiated by the game and is apart of the map
        StartSync();

    }

    void StartSync()
    {
        if (!synced)
        {
            if (!PhotonNetwork.isMasterClient)
            {
                photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, null);

            }
            synced = true;
        }
    }
        
}
