using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchShutters : MonoBehaviour
{

    public ShutterController[] shutters;

    private Animator animator;
    private bool closed = false;
    public PhotonView photonView;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnMouseDown()
    {
        if (PlayerManager.control.playerScript != null)
        {
            if (PlayerManager.control.playerScript.DistanceTo(transform.position) <= 2f)
            {
                if (!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Switches_ShuttersUP"))
                {
                    if (closed)
                    {
                        OpenShutters();
                        if (PhotonNetwork.connectedAndReady) //Send open to all other clients
                        {
                            CallRemoteMethod(true);
                        }
                    }
                    else
                    {
                        CloseShutters();
                        if (PhotonNetwork.connectedAndReady) //Send close to all other clients
                        {
                            CallRemoteMethod(false);
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

    //Photon RPC
    public void CallRemoteMethod(bool open)
    {
        if (open) //To open the shutters
        {
            photonView.RPC(
                "OpenShutters",
                PhotonTargets.OthersBuffered,
                null);
        }
        else //To close the shutters
        {
            photonView.RPC(
                "CloseShutters",
                PhotonTargets.OthersBuffered,
                null);
        }

    }
}
