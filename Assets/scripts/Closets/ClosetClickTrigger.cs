using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class ClosetClickTrigger: MonoBehaviour {

    public Sprite spriteClosed;
    public Sprite spriteOpened;

    private SpriteRenderer spriteRenderer;
    private GameObject lockLight;

    // Use this for initialization
    void Start() {
        spriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();

        Transform searchLockLight = transform.FindChild("LockLight");
        if(searchLockLight != null) {
            lockLight = searchLockLight.gameObject;
        }
    }

    // Update is called once per frame
    void Update() {

    }

    void OnMouseDown() {
        if(PlayerManager.control.playerScript != null) {
            var headingToPlayer = PlayerManager.control.playerScript.transform.position - transform.position;
            var distance = headingToPlayer.magnitude;

            if(distance <= 2f) {
                SoundManager.control.sounds[3].Play();
                if(spriteRenderer.sprite == spriteClosed) {
                    spriteRenderer.sprite = spriteOpened;
                    if(lockLight != null) {
                        lockLight.SetActive(false);
                    }
                } else {
                    spriteRenderer.sprite = spriteClosed;
                    if(lockLight != null) {
                        lockLight.SetActive(true);
                    }
                }
            }
        }
    }
}
