using Crafting;
using UnityEngine;
using UnityEngine.Networking;

public class Microwave : NetworkBehaviour
{
	private AudioSource audioSource;

	private float cookingTime;
	public float cookTime = 10;
	private string meal;
	private Sprite offSprite;
	public Sprite onSprite;

	private SpriteRenderer spriteRenderer;

	public bool Cooking { get; private set; }


	private void Start()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		audioSource = GetComponent<AudioSource>();
		offSprite = spriteRenderer.sprite;
	}

	private void Update()
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
			ItemFactory.Instance.SpawnMeal(mealPrefab, transform.position, transform.parent);
		}
		meal = null;
	}
}