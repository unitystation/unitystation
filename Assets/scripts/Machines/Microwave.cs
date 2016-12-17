using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using Events;

public class Microwave : MonoBehaviour {

    public Sprite onSprite;
    public float cookTime = 10;

    // temporary
    public GameObject dishPrefab;

    private SpriteRenderer spriteRenderer;
    private Sprite offSprite;
    private AudioSource audioSource;
    private bool cooking = false;
    private float cookingTime = 0;

    void Start() {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        offSprite = spriteRenderer.sprite;
    }

    void Update() {
        if(cooking) {
            cookingTime += Time.deltaTime;

            if(cookingTime >= cookTime) {
                StopCooking();
            }
        }
    }
    	
	void OnMouseDown() {
        var item = UIManager.control.hands.CurrentSlot.Item;

        if(!cooking && item) {
            var attr = item.GetComponent<ItemAttributes>();

            if(attr && attr.itemName == "Meat") {
                UIManager.control.hands.CurrentSlot.Clear();

                Destroy(item);

                StartCooking();
            }
        }
    }

    private void StartCooking() {
        cooking = true;
        cookingTime = 0;
        spriteRenderer.sprite = onSprite;
    }

    private void StopCooking() {
        cooking = false;
        spriteRenderer.sprite = offSprite;
        audioSource.Play();

        var dish = Instantiate(dishPrefab);
        dish.transform.position = transform.position;
    }
}
