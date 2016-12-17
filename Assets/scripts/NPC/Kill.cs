using PlayGroup;
using SS.NPC;
using UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kill : MonoBehaviour {

    public Sprite deadSprite;
    public GameObject meatPrefab;
    public int amountSpawn = 1;

    private SpriteRenderer spriteRenderer;
    private RandomMove randomMove;
    private PhysicsMove physicsMove;
    private bool dead = false;

	void Start () {
        spriteRenderer = GetComponent<SpriteRenderer>();
        randomMove = GetComponent<RandomMove>();
        physicsMove = GetComponent<PhysicsMove>();
    }

    void OnMouseDown() {
        if(!dead) {
            dead = true;
            randomMove.enabled = false;
            physicsMove.enabled = false;
            spriteRenderer.sprite = deadSprite;
            SoundManager.control.Play("Bodyfall");
        }else if(UIManager.control.hands.CurrentSlot.Item.GetComponent<ItemAttributes>().type == ItemType.Knife) {
            for(int i = 0; i < amountSpawn; i++) { 
                var meat = Instantiate(meatPrefab);
                meat.transform.position = transform.position;
            }
            Destroy(gameObject);
        }
    }
}
