using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using UI;

public class ExtinguisherCabinetTrigger: MonoBehaviour {

    public Sprite spriteClosed;
    public Sprite spriteOpenedOccupied;
    public Sprite spriteOpenedEmpty;

    public GameObject extinguisherPrefab;

    private GameObject extinguisher;
    private SpriteRenderer spriteRenderer;
    
    void Start() {
        spriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();
        extinguisher = Instantiate(extinguisherPrefab);
        extinguisher.transform.parent = transform;
        extinguisher.SetActive(false);
    }

    void OnMouseDown() {
        if(PlayerManager.control.playerScript != null) {
            if(PlayerManager.control.playerScript.DistanceTo(transform.position) <= 2) {
                SoundManager.control.Play("OpenClose");
                if(spriteRenderer.sprite == spriteClosed) {
                    OnOpen();
                } else {
                    OnClose();
                }
            }
        }
    }

    private void OnOpen() {
        if(extinguisher == null) {
            spriteRenderer.sprite = spriteOpenedEmpty;
        } else {
            spriteRenderer.sprite = spriteOpenedOccupied;
        }
    }

    private void OnClose() {
        if(extinguisher == null) {
            var item = UIManager.control.hands.currentSlot.Item;
            if(item != null && IsExtinguisher(item)) {
                extinguisher = item;
                UIManager.control.hands.currentSlot.RemoveItem();
                extinguisher.SetActive(false);
                spriteRenderer.sprite = spriteOpenedOccupied;
            } else {
                spriteRenderer.sprite = spriteClosed;
            }
        } else if(Items.ItemManager.control.TryToPickUpObject(extinguisher)) {
            // remove extinguisher from closet
            extinguisher.SetActive(true);
            extinguisher = null;
            spriteRenderer.sprite = spriteOpenedEmpty;
        }
    }

    private bool IsExtinguisher(GameObject item) {
        return item.GetComponent<ItemAttributes>().itemName == extinguisherPrefab.GetComponent<ItemAttributes>().itemName;
    }
}
