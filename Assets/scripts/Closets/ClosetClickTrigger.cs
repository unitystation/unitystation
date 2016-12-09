using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class ClosetClickTrigger: MonoBehaviour {

    private GameObject doorClosed;
    private GameObject doorOpened;
    private GameObject lockLight;

    private bool closed = true;

    // Use this for initialization
    void Start() {
        doorClosed = transform.FindChild("DoorClosed").gameObject;
        doorOpened = transform.FindChild("DoorOpened").gameObject;
        lockLight = transform.FindChild("LockLight").gameObject;
    }

    // Update is called once per frame
    void Update() {

    }
}
