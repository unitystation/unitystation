using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using Events;
using Crafting;

public class Microwave : NetworkBehaviour
{

    public Sprite onSprite;
    public float cookTime = 10;

    private SpriteRenderer spriteRenderer;
    private Sprite offSprite;
    private AudioSource audioSource;

    public bool Cooking { get; private set; }

    private float cookingTime = 0;
    private string meal;


    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        offSprite = spriteRenderer.sprite;
    }

    void Update()
    {
        if (Cooking)
        {
            cookingTime += Time.deltaTime;

            if (cookingTime >= cookTime)
            {
                StopCooking();
            }
        }
    }

    public void ServerSetOutputMeal(string mealName)
    {
        meal = mealName;
    }

    [ClientRpc]
    public void RpcStartCooking()
    {
        Cooking = true;
        cookingTime = 0;
        spriteRenderer.sprite = onSprite;
    }

    private void StopCooking()
    {
        Cooking = false;
        spriteRenderer.sprite = offSprite;
        audioSource.Play();
        if (isServer)
        {
            GameObject mealPrefab = CraftingManager.Meals.FindOutputMeal(meal);
            GameObject newMeal = Instantiate(mealPrefab, transform.position, Quaternion.identity) as GameObject;
            NetworkServer.Spawn(newMeal);
        }
        meal = null;
    }
}
