
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Encapsulates the various cooldowns a player has. On clientside, these are only tracked for the local player.
/// On server side, they are tracked for each player.
/// </summary>
[Serializable]
public class Cooldowns
{
	private static readonly float DefaultCooldownTime = 0.05f;

	[Tooltip("Time in seconds for each type of cooldown")]
	[SerializeField] private CooldownDictionary cooldownTimes;

	/// <summary>
	/// map from cooldown type to a bool which is true if the player is currently on cooldown for that cooldown type.
	/// </summary>
	private Dictionary<CooldownType, bool> cooldownTypeToCooldown = new Dictionary<CooldownType, bool>();

	/// <summary>
	/// Start the cooldown if not currently on cooldown.
	/// </summary>
	/// <param name="cooldownType">type of cooldown to start</param>
	/// <param name="owner">monobehavior which will be used to run the coroutine for handling the cooldown.</param>
	/// <returns>true if cooldown was started. False if already on cooldown.</returns>
	public bool TryStartCooldown(CooldownType cooldownType, MonoBehaviour owner)
	{
		if (IsOnCooldown(cooldownType)) return false;
		owner.StartCoroutine(InteractionCooldown(cooldownType));
		return true;
	}

	/// <summary>
	/// Is the indicated cooldown type currently on cooldown?
	/// </summary>
	/// <param name="cooldownType"></param>
	/// <returns></returns>
	public bool IsOnCooldown(CooldownType cooldownType)
	{
		if (cooldownTypeToCooldown.TryGetValue(cooldownType, out var isOnCooldown))
		{
			return isOnCooldown;
		}

		return false;
	}

	private IEnumerator InteractionCooldown(CooldownType cooldownType)
	{
		cooldownTypeToCooldown[cooldownType] = true;
		if (cooldownTimes.TryGetValue(cooldownType, out var cooldownTime))
		{
			yield return WaitFor.Seconds(cooldownTime);
		}
		else
		{
			yield return WaitFor.Seconds(DefaultCooldownTime);
		}
		cooldownTypeToCooldown[cooldownType] = false;
	}



}

/// <summary>
/// different kinds of cooldowns.
/// </summary>
public enum CooldownType
{
	//Non-attack interactions
	Interaction = 0,
	//Melee type interactions
	Melee = 1
}
