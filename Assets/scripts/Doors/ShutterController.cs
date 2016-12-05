using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class ShutterController : MonoBehaviour {
    private Animator animator;
    private bool isClosed = false;
    //private BoxCollider2D boxColl;

    // Use this for initialization
    void Start() {
        animator = gameObject.GetComponent<Animator>();
        //boxColl = gameObject.GetComponent<BoxCollider2D>();
    }

    void Update() {
    }

    //public void BoxCollToggleOn() {
    //    boxColl.enabled = true;
    //}

    //public void BoxCollToggleOff() {
    //    boxColl.enabled = false;
    //}

    public void Open() {
        isClosed = false;
        animator.SetBool("close", false);
    }

    public void Close() {
        isClosed = true;
        animator.SetBool("close", true);
    }
}
