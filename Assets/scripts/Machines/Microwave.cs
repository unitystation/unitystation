using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using Events;
using Crafting;

public class Microwave : MonoBehaviour {

    public Sprite onSprite;
    public float cookTime = 10;

    private SpriteRenderer spriteRenderer;
    private Sprite offSprite;
    private AudioSource audioSource;

    private bool cooking = false;
    private float cookingTime = 0;
    private GameObject mealPrefab = null;


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

            var ingredient = new Ingredient(attr.itemName);
            
            var meal = CraftingManager.Instance.Meals.FindRecipe(new List<Ingredient>() { ingredient });

            if(meal) {
                UIManager.control.hands.CurrentSlot.Clear();

                Destroy(item);

                StartCooking(meal);
            }
        }
    }

    private void StartCooking(GameObject meal) {
        cooking = true;
        cookingTime = 0;
        spriteRenderer.sprite = onSprite;
        mealPrefab = meal;
    }

    private void StopCooking() {
        cooking = false;
        spriteRenderer.sprite = offSprite;
        audioSource.Play();

        var dish = Instantiate(mealPrefab);
        dish.transform.position = transform.position;
        mealPrefab = null;
    }
}
