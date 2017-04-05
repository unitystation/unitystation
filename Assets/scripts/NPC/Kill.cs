using PlayGroup;
using NPC;
using UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Network;

public class Kill: NetworkBehaviour{

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
                    //Send death to all clients for pete
                } else if(!sliced) {
                   //Spawn the new meat

                   // Remove pete from the network

                    sliced = true;
                }
            }
        }
    }

    [PunRPC]
    void Die() {
		Debug.Log("FIXME Kill.cs");
//        dead = true;
//        randomMove.enabled = false;
//        physicsMove.enabled = false;
//        spriteRenderer.sprite = deadSprite;
//        SoundManager.Play("Bodyfall", 0.5f);
    }

    [PunRPC]
    void Gib() {
		Debug.Log("FIXME Kill.cs");
//        if(PhotonNetwork.isMasterClient) {
//            // Spawn Meat
//            for(int i = 0; i < amountSpawn; i++) {
//                NetworkItemDB.Instance.MasterClientCreateItem(meatPrefab.name, transform.position); //Create scene owned object
//            }
//
//            // Spawn Corpse
//            NetworkItemDB.Instance.MasterClientCreateItem(corpsePrefab.name, transform.position); //Create scene owned object
//
//        }
    }

    [PunRPC]
    void RemoveFromNetwork() //Can only be called by masterclient
    {
		Debug.Log("FIXME Kill.cs");
//        PhotonNetwork.Destroy(this.gameObject);
    }
}
