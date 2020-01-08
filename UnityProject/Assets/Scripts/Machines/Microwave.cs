using UnityEngine;
using Mirror;

/// <summary>
/// A machine into which players can insert certain food items.
/// After some time the food is cooked and gets ejected from the microwave.
/// </summary>
public class Microwave : NetworkBehaviour
{
	/// <summary>
	/// Time it takes for the microwave to cook a meal (in seconds).
	/// </summary>
	[Range(0, 60)]
	public float COOK_TIME = 10;

	/// <summary>
	/// Time left until the meal has finished cooking (in seconds).
	/// </summary>
	[HideInInspector]
	public float microwaveTimer = 0;

	/// <summary>
	/// Meal currently being cooked.
	/// </summary>
	[HideInInspector]
	public string meal;

	// Sprites for when the microwave is on or off.
	public Sprite SPRITE_ON;
	private Sprite SPRITE_OFF;

	private SpriteRenderer spriteRenderer;

	/// <summary>
	/// AudioSource for playing the celebratory "ding" when cooking is finished.
	/// </summary>
	private AudioSource audioSourceDing;

	/// <summary>
	/// Amount of time the "ding" audio source will start playing before the microwave has finished cooking (in seconds).
	/// </summary>
	private static float dingPlayTime = 1.54f;

	/// <summary>
	/// True if the microwave has already played the "ding" sound for the current meal.
	/// </summary>
	private bool dingHasPlayed = false;

	/// <summary>
	/// Set up the microwave sprite and the AudioSource.
	/// </summary>
	private void Start()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		audioSourceDing = GetComponent<AudioSource>();
		SPRITE_OFF = spriteRenderer.sprite;
	}

	/// <summary>
	/// Count remaining time to microwave previously inserted food.
	/// </summary>
	private void Update()
	{
		if (microwaveTimer > 0)
		{
			microwaveTimer = Mathf.Max(0, microwaveTimer - Time.deltaTime);

			if (!dingHasPlayed && microwaveTimer <= dingPlayTime)
			{
				audioSourceDing.Play();
				dingHasPlayed = true;
			}

			if (microwaveTimer <= 0)
			{
				FinishCooking();
			}
		}
	}

	public void ServerSetOutputMeal(string mealName)
	{
		meal = mealName;
	}

	/// <summary>
	/// Starts the microwave, cooking the food for {microwaveTimer} seconds.
	/// </summary>
	[ClientRpc]
	public void RpcStartCooking()
	{
		microwaveTimer = COOK_TIME;
		spriteRenderer.sprite = SPRITE_ON;
		dingHasPlayed = false;
	}

	/// <summary>
	/// Finish cooking the microwaved meal.
	/// </summary>
	private void FinishCooking()
	{
		spriteRenderer.sprite = SPRITE_OFF;
		if (isServer)
		{
			GameObject mealPrefab = CraftingManager.Meals.FindOutputMeal(meal);
			Spawn.ServerPrefab(mealPrefab, GetComponent<RegisterTile>().WorldPosition, transform.parent);
		}
		meal = null;
	}

}