using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using UI;
using Network;
using Items;
using InputControl;

public class CabinetTrigger: InputTrigger {
    public Sprite spriteClosed;
    public Sprite spriteOpenedOccupied;
    public Sprite spriteOpenedEmpty;

    public GameObject itemPrefab;

    public bool IsClosed { get; private set; }
    public int ItemViewID { get; private set; }

    private SpriteRenderer spriteRenderer;

    void Start() {
        spriteRenderer = transform.FindChild("Sprite").GetComponent<SpriteRenderer>();
        IsClosed = true;

        ItemViewID = transform.FindChild("Extinguisher").GetComponent<PhotonView>().viewID;
    }

    public override void Interact() {
        if(IsClosed) {
            photonView.RPC("SyncState", PhotonTargets.All, false, ItemViewID, true);
        } else {
            OnClose();
        }
    }

    private void OnClose() {
        if(ItemViewID < 0) {
            var item = UIManager.Hands.CurrentSlot.Item;
            if(item != null && IsCorrectItem(item)) {
                var itemViewID = item.GetComponent<PhotonView>().viewID;
                ItemViewID = itemViewID;
                item.SetActive(false);
                item.transform.parent = transform;
                UIManager.Hands.CurrentSlot.Clear();
                photonView.RPC("SyncState", PhotonTargets.All, IsClosed, itemViewID, false);
            } else {
                photonView.RPC("SyncState", PhotonTargets.All, true, ItemViewID, true);
            }
        } else {
            var item = transform.FindChild("Extinguisher").gameObject;
            if(ItemManager.TryToPickUpObject(item)) {
                // remove extinguisher from closet
                item.SetActive(true);
                photonView.RPC("SyncState", PhotonTargets.All, IsClosed, -1, false);
            }
        }
    }

    [PunRPC]
    public void SyncState(bool isClosed, int itemViewID, bool playSound) {
        IsClosed = isClosed;

        ItemViewID = itemViewID;

        if(ItemViewID >= 0)
            if(NetworkItemDB.Items.ContainsKey(ItemViewID)) {
                NetworkItemDB.Items[ItemViewID].SetActive(false);
            }

        if(playSound) SoundManager.Play("OpenClose");

        UpdateSprite();
    }

    private void UpdateSprite() {
        if(IsClosed) {
            spriteRenderer.sprite = spriteClosed;
        } else if(ItemViewID >= 0) {
            spriteRenderer.sprite = spriteOpenedOccupied;
        } else {
            spriteRenderer.sprite = spriteOpenedEmpty;
        }
    }

    private bool IsCorrectItem(GameObject item) {
        return item.GetComponent<ItemAttributes>().itemName == itemPrefab.GetComponent<ItemAttributes>().itemName;
    }
}