using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used by the Metabolism class to apply the desired effects. Substances are applied evenly over the duration of the effect.
/// </summary>
public struct MetabolismEffect
{
	public int totalNutrients;
	public int totalToxins;
	public int initialDuration;
	public int duration;

	public MetabolismEffect(int nutrientAmount, int toxinAmount, MetabolismDuration duration)
	{
		this.totalNutrients = nutrientAmount;
		this.totalToxins = toxinAmount;
		this.initialDuration = (int) duration;
		this.duration = (int) duration;
	}
}

public enum HungerState
{
	Full,
	Normal,
	Hungry,
	Malnourished,
	Starving

}

public enum MetabolismDuration
{
	Food = 3
}

/// <summary>
/// Handles the intake of substances (food, drink, chemical reagents etc...) for a Living Entity
/// </summary>
public class MetabolismSystem : NetworkBehaviour
{
	public static int MAX_NUTRITION_LEVEL = 200;

	//TODO: Maybe make this dependent on the heart rate?
	[SerializeField]
	[Tooltip("How often a metabolism tick occurs (in seconds)")]
	private float metabolismRate = 5f;

	[SerializeField]
	[Tooltip("How fast the entity can walk while in the starving state")]
	private float starvingWalkSpeed = 2f;

	public int NutritionLevel => nutritionLevel;

	private int nutritionLevel = 125;
	public HungerState HungerState => hungerState;

	[SyncVar]
	private HungerState hungerState = HungerState.Normal;

	public bool IsHungry => HungerState >= HungerState.Hungry;
	public bool IsStarving => HungerState == HungerState.Starving;

	/// <summary>
	/// How much hunger is applied per metabolism tick
	/// </summary>
	public int HungerRate { get; set; } = 1;

	private BloodSystem bloodSystem;
	private PlayerMove playerMove;
	private List<MetabolismEffect> effects;
	private float tick = 0;

	void Awake()
	{
		bloodSystem = GetComponent<BloodSystem>();
		playerMove = GetComponent<PlayerMove>();
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
		effects = new List<MetabolismEffect>();
	}
	void OnDisable()
	{
		if (UpdateManager.Instance != null)
			UpdateManager.Instance.Remove(UpdateMe);
	}

	void UpdateMe()
    {
		if (CustomNetworkManager.Instance._isServer)
		{
			tick += Time.deltaTime;

			if (tick >= metabolismRate && !bloodSystem.HeartStopped) //Metabolism tick
			{
				//Apply hunger
				nutritionLevel -= HungerRate;

				//Apply effects
				for(int i = effects.Count - 1; i >= 0; i--)
				{
					MetabolismEffect e = effects[i];

					if (e.duration <= 0)
					{
						effects.RemoveAt(i);
						continue;
					}

					nutritionLevel += e.totalNutrients / e.initialDuration;
					bloodSystem.ToxinLevel += e.totalToxins / e.initialDuration;
					e.duration--;
					effects[i] = e;
				}

				nutritionLevel = Mathf.Clamp(nutritionLevel, 0, MAX_NUTRITION_LEVEL);

				if (nutritionLevel > 150) //TODO: Make character nauseous when he's too full
				{
					if(HungerState != HungerState.Full)
						Chat.AddExamineMsgFromServer(gameObject, "You're stuffed!");

					hungerState = HungerState.Full;
				}
				else if (nutritionLevel > 75)
				{
					if(HungerState != HungerState.Normal)
						Chat.AddExamineMsgFromServer(gameObject, "You're satiated.");

					hungerState = HungerState.Normal;
				}
				else if (nutritionLevel > 25)
				{
					if(HungerState != HungerState.Hungry)
						Chat.AddExamineMsgFromServer(gameObject, "You feel hungry.");

					hungerState = HungerState.Hungry;
				}
				else if (nutritionLevel > 0)
				{
					if(HungerState != HungerState.Malnourished)
						Chat.AddWarningMsgFromServer(gameObject, "Your stomach rumbles violently.");

					hungerState = HungerState.Malnourished;
				}
				else if (HungerState != HungerState.Starving)
				{
					Chat.AddWarningMsgFromServer(gameObject, "Your malnourished body aches!");
					hungerState = HungerState.Starving;
				}


				if (HungerState == HungerState.Starving)
				{
					playerMove.RunSpeed = playerMove.WalkSpeed;
				}
				else
				{
					playerMove.RunSpeed = playerMove.InitialRunSpeed;
				}

				tick = 0;
			}
		}
    }

	/// <summary>
	/// Adds a MetabolismEffect to the system. The effect is applied every metabolism tick.
	/// </summary>
	public void AddEffect(MetabolismEffect effect)
	{
		effects.Add(effect);
	}
}