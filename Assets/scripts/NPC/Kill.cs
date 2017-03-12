using PlayGroup;
using SS.NPC;
using UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;

public class Kill: Photon.MonoBehaviour {

    public Sprite deadSprite;
    public GameObject meatPrefab;
    public GameObject corpsePrefab;
    public int amountSpawn = 1;

    private SpriteRenderer spriteRenderer;
    private RandomMove randomMove;
    private PhysicsMove physicsMove;
    private bool dead = false;
    private bool sliced = false;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        randomMove = GetComponent<RandomMove>();
        physicsMove = GetComponent<PhysicsMove>();
    }

    void OnMouseDown() {
        if(UIManager.Hands.CurrentSlot.Item != null && PlayerManager.PlayerInReach(transform)) {
            if(UIManager.Hands.CurrentSlot.Item.GetComponent<ItemAttributes>().type == ItemType.Knife) {
                if(!dead) {
                    photonView.RPC("Die", PhotonTargets.All, null); //Send death to all clients for pete
                } else if(!sliced) {
                    photonView.RPC("Gib", PhotonTargets.MasterClient, null); //Spawn the new meat
                    photonView.RPC("RemoveFromNetwork", PhotonTargets.MasterClient, null); // Remove pete from the network

                    sliced = true;
                }
            }
        }
    }

    [PunRPC]
    void Die() {
        dead = true;
        randomMove.enabled = false;
        physicsMove.enabled = false;
        spriteRenderer.sprite = deadSprite;
        SoundManager.Play("Bodyfall", 0.5f);
    }

    [PunRPC]
    void Gib() {
        if(PhotonNetwork.isMasterClient) {
            // Spawn Meat
            for(int i = 0; i < amountSpawn; i++) {
                NetworkItemDB.Instance.MasterClientCreateItem(meatPrefab.name, transform.position); //Create scene owned object
            }

            // Spawn Corpse
            NetworkItemDB.Instance.MasterClientCreateItem(corpsePrefab.name, transform.position); //Create scene owned object

        }
    }

    [PunRPC]
    void RemoveFromNetwork() //Can only be called by masterclient
    {
        PhotonNetwork.Destroy(this.gameObject);
    }
}
