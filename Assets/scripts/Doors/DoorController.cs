using UnityEngine;
using System.Collections;
using PlayGroup;

public class DoorController: MonoBehaviour {

    /* QUICK MOCK UP
	 * OF DOOR SYSTEM
	 */

    private Animator thisAnim;
    private BoxCollider2D boxColl;
    private bool isOpened = false;

    public float maxTimeOpen = 5;
    private float timeOpen = 0;
    private int numOccupiers = 0;

    // Use this for initialization
    void Start() {
        thisAnim = gameObject.GetComponent<Animator>();
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

    public void PlayCloseSound() {
        SoundManager.control.sounds[2].Play();
    }

    public void PlayCloseSFXshort() {
        if(SoundManager.control != null) {
            SoundManager.control.sounds[2].time = 0.6f;
            SoundManager.control.sounds[2].Play();
        }
    }

    private void Open() {
        isOpened = true;
        thisAnim.SetBool("open", true);
        SoundManager.control.sounds[1].Play();
    }

    private void Close() {
        isOpened = false;
        thisAnim.SetBool("open", false);
    }
}
