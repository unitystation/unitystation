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
	/// I don't see the need to make it a SyncVar.  It will economize network packets.
	/// </summary>
	[HideInInspector]
	public float MicrowaveTimer = 0;

	/// <summary>
	/// Meal currently being cooked.
	/// </summary>
	[HideInInspector]
	public string meal;

	/// <summary>
	/// When a stackable food goes into the microwave, hold onto the stack number so that stack.amount in = stack.amount out
	/// </summary>
	[HideInInspector]
	public int mealCount = 0;


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
		if (MicrowaveTimer > 0)
		{
			MicrowaveTimer = Mathf.Max(0, MicrowaveTimer - Time.deltaTime);

			if (!dingHasPlayed && MicrowaveTimer <= dingPlayTime)
			{
				audioSourceDing.Play();
				dingHasPlayed = true;
			}

			if (MicrowaveTimer <= 0)
			{
				FinishCooking();
			}
		}
	}

	public void ServerSetOutputMeal(string mealName)
	{
		meal = mealName;
	}

		public void ServerSetOutputStackAmount(int stackCount)
	{
		mealCount = stackCount;
	}

	/// <summary>
	/// Starts the microwave, cooking the food for {MicrowaveTimer} seconds.
	/// </summary>
	[ClientRpc]
	public void RpcStartCooking()
	{
		MicrowaveTimer = COOK_TIME;
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
			SpawnResult result = Spawn.ServerPrefab(mealPrefab, GetComponent<RegisterTile>().WorldPosition, transform.parent);
			
			//If the resulting meal has a stackable component, set the amount to mealCount to ensure that food in = food out.
			Stackable stck = result.GameObject.GetComponent<Stackable>();

			if (stck != null && mealCount != 0)
			{
				//Get difference between new item's initial amount and the amount held by mealCount (amount of ingredient).
				int stckChanger = mealCount-stck.Amount;

				//If stckChanger is 0, do nothing.
				//If stckChanger is positive, add to stack.
				if (stckChanger > 0)
				{
					stck.ServerIncrease(stckChanger);
				} else if (stckChanger < 0)
				{
					//If stckChanger is positive, remove stack.
					stck.ServerConsume(-stckChanger);
				}
				
			}



		}
		meal = null;
		mealCount = 0;
	}

}