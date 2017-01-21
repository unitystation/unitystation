using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchShutters: Photon.PunBehaviour {

    public ShutterController[] shutters;

    public bool IsClosed { get; private set; }

    private Animator animator;

    void Start() {
        animator = GetComponent<Animator>();
    }

    void OnMouseDown() {
        if(PlayerManager.PlayerInReach(transform)) {
            if(!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Switches_ShuttersUP")) {
                if(IsClosed) {
                    photonView.RPC("OpenShutters", PhotonTargets.All, null);
                } else {
                    photonView.RPC("CloseShutters", PhotonTargets.All, null);
                }
            }
        }
    }

    [PunRPC]
    public void OpenShutters() {
        if(IsClosed) {
            IsClosed = false;
            foreach(var s in shutters) {
                s.Open();
            }
            animator.SetTrigger("activated");
        }
    }

    [PunRPC]
    public void CloseShutters() {
        if(!IsClosed) {
            IsClosed = true;
            foreach(var s in shutters) {
                s.Close();
            }
            animator.SetTrigger("activated");
        }
    }
}
