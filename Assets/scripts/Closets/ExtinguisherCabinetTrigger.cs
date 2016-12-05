using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class ExtinguisherCabinetTrigger: MonoBehaviour {

    public Sprite spriteClosed;
    public Sprite spriteOpenedOccupied;
    public Sprite spriteOpenedEmpty;

    public GameObject extinguisherPrefab;
    public GameObject extinguisher;

    private SpriteRenderer spriteRenderer;

    // Use this for initialization
    void Start() {
        spriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();
        extinguisher = Instantiate(extinguisherPrefab);
        extinguisher.SetActive(false);
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
                    if(extinguisher == null) {
                        spriteRenderer.sprite = spriteOpenedEmpty;
                    } else {
                        spriteRenderer.sprite = spriteOpenedOccupied;
                    }
                } else {
                    if(extinguisher == null) {
                        spriteRenderer.sprite = spriteClosed;
                    } else if(Items.ItemManager.control.TryToPickUpObject(extinguisher)) {
                        extinguisher.SetActive(true);
                        extinguisher = null;
                        spriteRenderer.sprite = spriteOpenedEmpty;
                    }
                }
            }
        }
    }
}
