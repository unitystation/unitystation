using UnityEngine;
using System.Collections;
using PlayGroup;

public class DoorController: MonoBehaviour {
    private Animator animator;
    private BoxCollider2D boxColl;
    private bool isOpened = false;

    public float maxTimeOpen = 5;
    private float timeOpen = 0;
    private int numOccupiers = 0;

    // Use this for initialization
    void Start() {
        animator = gameObject.GetComponent<Animator>();
        boxColl = gameObject.GetComponent<BoxCollider2D>();
    }

    void Update() {
        waitUntilClose();
    }

    public void BoxCollToggleOn() {
        boxColl.enabled = true;
    }

    public void BoxCollToggleOff() {
        boxColl.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D coll) {
        if(!isOpened && coll.gameObject.layer == 8) {
            Open();
        }
        numOccupiers++;
    }

    void OnTriggerExit2D(Collider2D coll) {
        numOccupiers--;
    }

    private void waitUntilClose() {
        if(isOpened && numOccupiers == 0) {
            timeOpen += Time.deltaTime;

            if(timeOpen >= maxTimeOpen) {
                Close();
            }
        }else {
            timeOpen = 0;
        }
    }
    
    public void PlayOpenSound() {
        SoundManager.control.sounds[1].Play();
    }

    public void PlayCloseSound() {
        SoundManager.control.sounds[2].Play();
    }

    public void PlayCloseSFXshort() {
        if(SoundManager.control != null) {
            SoundManager.control.sounds[2].time = 0.6f;
            SoundManager.control.sounds[2].Play();
        }
    }

    void OnMouseDown() {
        Debug.Log("hello");
        if(PlayerManager.control.playerScript != null) {
            var headingToPlayer = PlayerManager.control.playerScript.transform.position - transform.position;
            var distance = headingToPlayer.magnitude;

            if(distance <= 2f) {
                if(isOpened) {
                    Close();
                } else {
                    Open();
                }
            }
        }
    }

    private void Open() {
        isOpened = true;
        animator.SetBool("open", true);
    }

    private void Close() {
        isOpened = false;
        animator.SetBool("open", false);
    }
}
