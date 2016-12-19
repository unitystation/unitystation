using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using Events;
using Crafting;

public class Microwave : MonoBehaviour
{

	public Sprite onSprite;
	public float cookTime = 10;

	private SpriteRenderer spriteRenderer;
	private Sprite offSprite;
	private AudioSource audioSource;

	private bool cooking = false;
	private float cookingTime = 0;
	private GameObject mealPrefab = null;
	private string mealName;
	private PhotonView photonView;

	void Awake ()
	{
		photonView = gameObject.GetComponent<PhotonView> ();
	}

	void Start ()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer> ();
		audioSource = GetComponent<AudioSource> ();
		offSprite = spriteRenderer.sprite;
	}

	void Update ()
	{
		if (cooking) {
			cookingTime += Time.deltaTime;

			if (cookingTime >= cookTime) {
				StopCooking ();
			}
		}
	}

	void OnMouseDown ()
	{
		var item = UIManager.control.hands.CurrentSlot.Item;

		if (!cooking && item) {
			var attr = item.GetComponent<ItemAttributes> ();

			var ingredient = new Ingredient (attr.itemName);
            
			var meal = CraftingManager.Instance.Meals.FindRecipe (new List<Ingredient> () { ingredient });

			if (meal) {
				UIManager.control.hands.CurrentSlot.Clear ();

				if (PhotonNetwork.connectedAndReady) {
					PhotonView itemView = item.GetComponent<PhotonView> ();
					GameMatrix.control.RemoveItem (itemView.viewID); //Remove ingredients from all clients
					photonView.RPC ("StartCookingRPC", PhotonTargets.All, meal.name);
				} else {//Dev mode
					Destroy (item);
					StartCooking (meal);
				}
			}
		}
	}

	[PunRPC]
	void StartCookingRPC (string meal)
	{
		cooking = true;
		cookingTime = 0;
		spriteRenderer.sprite = onSprite;
		mealName = meal;

	}

	private void StartCooking (GameObject meal) //for dev mode
	{
		cooking = true;
		cookingTime = 0;
		spriteRenderer.sprite = onSprite;
		mealPrefab = meal;
	}

	private void StopCooking ()
	{
		cooking = false;
		spriteRenderer.sprite = offSprite;
		audioSource.Play ();
		if (PhotonNetwork.connectedAndReady) {
			GameMatrix.control.InstantiateItem (mealName, transform.position, Quaternion.identity, 0, null);
			mealName = null;
		} else {//Dev mode
			var dish = Instantiate (mealPrefab);
			dish.transform.position = transform.position;
			mealPrefab = null;
		}
	}
}
