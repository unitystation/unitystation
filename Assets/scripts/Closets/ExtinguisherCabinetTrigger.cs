using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class ExtinguisherCabinetTrigger: MonoBehaviour {

    public Sprite spriteClosed;
    public Sprite spriteOpenedOccupied;
    public Sprite spriteOpenedEmpty;

    public GameObject extinguisherPrefab;

    private bool empty = false;

    private SpriteRenderer spriteRenderer;

    // Use this for initialization
    void Start () {
        spriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnMouseDown() {
        if(PlayerManager.control.playerScript != null) {
            var headingToPlayer = PlayerManager.control.playerScript.transform.position - transform.position;
            var distance = headingToPlayer.magnitude;

            if(distance <= 2f) {
                SoundManager.control.sounds[3].Play();
                if(spriteRenderer.sprite == spriteClosed) {
                    if(empty) {
                        spriteRenderer.sprite = spriteOpenedEmpty;
                    } else {
                        spriteRenderer.sprite = spriteOpenedOccupied;
                    }
                } else {
                    if(empty) { 
                        spriteRenderer.sprite = spriteClosed;

                    }else {
                        empty = true;
                        Items.ItemManager.control.PickUpObject(Instantiate(extinguisherPrefab));
                        spriteRenderer.sprite = spriteOpenedEmpty;
                    }
                }
            }
        }
    }
}
