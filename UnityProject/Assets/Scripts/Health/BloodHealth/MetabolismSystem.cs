using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
	public static int NUTRITION_LEVEL_MAX = 500;
	public static int NUTRITION_LEVEL_STUFFED = 450;
	public static int NUTRITION_LEVEL_NORMAL = 300;
	public static int NUTRITION_LEVEL_HUNGRY = 200;
	public static int NUTRITION_LEVEL_MALNOURISHED = 100;
	public static int NUTRITION_LEVEL_STARVING = 0;

	//TODO: Maybe make this dependent on the heart rate?
	[SerializeField]
	[Tooltip("How often a metabolism tick occurs (in seconds)")]
	private float metabolismRate = 5f;

	[SerializeField]
	[Tooltip("Speed debuff when running and starving")]
	private float starvingRunDebuff = 2f;

	[SerializeField]
	[Tooltip("Speed debuff when walking and starving")]
	private float starvingWalkDebuff = 1f;
	
	public int NutritionLevel => nutritionLevel;

	private int nutritionLevel = 400;
	public HungerState HungerState { get; private set; }

	public bool IsHungry => HungerState >= HungerState.Hungry;
	public bool IsStarving => HungerState == HungerState.Starving;

	/// <summary>
	/// How much hunger is applied per metabolism tick
	/// </summary>
	public int HungerRate { get; private set; } = 1;

	private BloodSystem bloodSystem;
	private PlayerMove playerMove;
	private List<MetabolismEffect> effects;
	private bool appliedStarvingDebuff;

	private void Awake()
	{
		bloodSystem = GetComponent<BloodSystem>();
		playerMove = GetComponent<PlayerMove>();
	}

	private void OnEnable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(ServerUpdateMe, metabolismRate);
		}

		effects = new List<MetabolismEffect>();
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerUpdateMe);
		}
	}

	// Metabolism tick
	private void ServerUpdateMe()
	{
		if (bloodSystem.HeartStopped) return; 

		//Apply hunger
		nutritionLevel -= HungerRate;

		//Apply effects
		for (int i = effects.Count - 1; i >= 0; i--)
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

		nutritionLevel = Mathf.Clamp(nutritionLevel, 0, NUTRITION_LEVEL_MAX);

		HungerState oldState = this.HungerState;

		if (nutritionLevel > NUTRITION_LEVEL_STUFFED) //TODO: Make character nauseous when he's too full
			HungerState = HungerState.Full;
		else if (nutritionLevel > NUTRITION_LEVEL_NORMAL)
			HungerState = HungerState.Normal;
		else if (nutritionLevel > NUTRITION_LEVEL_HUNGRY)
			HungerState = HungerState.Hungry;
		else if (nutritionLevel > NUTRITION_LEVEL_MALNOURISHED)
			HungerState = HungerState.Malnourished;
		else
			HungerState = HungerState.Starving;

		if (oldState != this.HungerState)
		{
			SetHungerState(HungerState);
			// HungerState was altered, send new one to player
			UpdateHungerStateMessage.Send(this.gameObject, HungerState);
		}
	}

	public void SetHungerState(HungerState newState)
	{
		if (newState == HungerState) return;
		HungerState = newState;

		if (IsStarving)
		{
			ApplySpeedDebuff();
		}
		else
		{
			RemoveSpeedDebuff();
		}

		if (PlayerManager.LocalPlayer == gameObject)
		{
			Chat.AddExamineMsgToClient(GetHungerMessage());
		}
	}

	private string GetHungerMessage()
	{
		switch (HungerState)
		{
			case HungerState.Full:
				return "You're stuffed!";
			case HungerState.Normal:
				return "You're satiated.";
			case HungerState.Hungry:
				return "You feel hungry.";
			case HungerState.Malnourished:
				return "Your stomach rumbles violently.";
			case HungerState.Starving:
				return "Your malnourished body aches!";
		}

		return default;
	}

	/// <summary>
	/// Applies the speed debuff when starving
	/// </summary>
	private void ApplySpeedDebuff()
	{
		if (appliedStarvingDebuff) return;

		playerMove.ServerChangeSpeed(
			run: playerMove.RunSpeed - starvingRunDebuff,
			walk: playerMove.WalkSpeed - starvingWalkDebuff);
		appliedStarvingDebuff = true;
	}

	/// <summary>
	/// Removes the speed debuff when starving
	/// </summary>
	private void RemoveSpeedDebuff()
	{
		if (!appliedStarvingDebuff) return;

		playerMove.ServerChangeSpeed(
			run: playerMove.RunSpeed + starvingRunDebuff,
			walk: playerMove.WalkSpeed + starvingWalkDebuff);
		appliedStarvingDebuff = false;
	}

	/// <summary>
	/// Adds a MetabolismEffect to the system. The effect is applied every metabolism tick.
	/// </summary>
	public void AddEffect(MetabolismEffect effect)
	{
		effects.Add(effect);
	}
}
