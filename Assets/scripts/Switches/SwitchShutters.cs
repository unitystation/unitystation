using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchShutters : MonoBehaviour {

    public ShutterController[] shutters;

    private Animator animator;

    private bool closed = false;

	// Use this for initialization
	void Start () {
        animator = GetComponent<Animator>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnMouseDown() {
        if(closed) {
            OpenShutters();
        } else {
            CloseShutters();
        }

        closed = !closed;
        animator.SetTrigger("pressed");
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
