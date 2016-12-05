using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchShutters : MonoBehaviour {

    public ShutterController[] shutters;

    private Animator animator;

    private bool closed = false;
    
	void Start () {
        animator = GetComponent<Animator>();
    }
	
	void Update () {
		
	}

    void OnMouseDown() {
        if(!this.animator.GetCurrentAnimatorStateInfo(0).IsName("Switches_ShuttersUP")) { 
            if(closed) {
                OpenShutters();
            } else {
                CloseShutters();
            }

            closed = !closed;
            animator.SetTrigger("activated");
        }
    }

    private void OpenShutters() {
        foreach(var s in shutters) {
            s.Open();
        }
    }

    private void CloseShutters() {
        foreach(var s in shutters) {
            s.Close();
        }
    }
}
