using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class DoorTrigger: MonoBehaviour {

    public Sprite doorOpened;
    public Vector2 offsetOpened;
    public Vector2 sizeOpened;

    private SpriteRenderer spriteRenderer;
    private Vector2 offsetClosed;
    private Vector2 sizeClosed;

    private Sprite doorClosed;
    private LockLightController lockLight;
    private GameObject items;

    private bool closed = true;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        doorClosed = spriteRenderer.sprite;
        lockLight = transform.GetComponentInChildren<LockLightController>();

        offsetClosed = GetComponent<BoxCollider2D>().offset;
        sizeClosed = GetComponent<BoxCollider2D>().size;

        items = transform.FindChild("Items").gameObject;
    }

    void OnMouseDown() {
        if(PlayerManager.control.playerScript != null) {
            var headingToPlayer = PlayerManager.control.playerScript.transform.position - transform.position;
            var distance = headingToPlayer.magnitude;

            if(distance <= 2f) {
                if(lockLight != null && lockLight.IsLocked()) {
                    lockLight.Unlock();
                } else {
                    SoundManager.control.Play("OpenClose");
                    if(closed) {
                        Open();
                    } else if(!TryDropItem()) {
                        Close();
                    }
                }
            }
        }
    }

    void Open() {
        closed = false;
        spriteRenderer.sprite = doorOpened;
        if(lockLight != null) {
            lockLight.Hide();
        }
        GetComponent<BoxCollider2D>().offset = offsetOpened;
        GetComponent<BoxCollider2D>().size = sizeOpened;

        ShowItems();
    }

    void Close() {
        closed = true;
        spriteRenderer.sprite = doorClosed;
        if(lockLight != null) {
            lockLight.Show();
        }
        GetComponent<BoxCollider2D>().offset = offsetClosed;
        GetComponent<BoxCollider2D>().size = sizeClosed;

        HideItems();
    }

    private bool TryDropItem() {
        GameObject item = UIManager.control.hands.CurrentSlot.Clear();

        if(item != null) {
            item.transform.parent = items.transform;
            item.transform.localPosition = new Vector3(0, 0, -0.2f);

            return true;
        }

        return false;
    }

    private void ShowItems() {
        items.SetActive(true);
    }

    private void HideItems() {
        items.SetActive(false);
    }
}
