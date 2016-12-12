using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class ShutterController : MonoBehaviour {
    private Animator animator;
    private bool isClosed = false;

    void Start() {
        animator = gameObject.GetComponent<Animator>();
    }

    public void Open() {
        isClosed = false;
        animator.SetBool("close", false);
    }

    public void Close() {
        isClosed = true;
        animator.SetBool("close", true);
    }
}
