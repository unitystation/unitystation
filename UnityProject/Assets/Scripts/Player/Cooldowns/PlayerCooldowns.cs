
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the various cooldowns a player has and allows configuring how long each should be. On clientside, these are only tracked for the local player.
/// On server side, they are tracked for each player.
/// </summary>
[Serializable]
public class PlayerCooldowns
{
	private static readonly float DefaultCooldownTime = 0.1f;

	[Tooltip("Time in seconds for each category of cooldown")]
	[SerializeField] private CooldownDictionary cooldownTimes;

	/// <summary>
	/// map from cooldown to a bool which is true if the player is currently on cooldown for that cooldown.
	/// </summary>
	private Dictionary<Cooldown, bool> cooldownTypeToCooldown = new Dictionary<Cooldown, bool>();

	/// <summary>
	/// Start the cooldown if not currently on cooldown.
	/// </summary>
	/// <param name="cooldown">cooldown to start</param>
	/// <param name="owner">monobehavior which will be used to run the coroutine for handling the cooldown.</param>
	/// <returns>true if cooldown was started. False if already on cooldown.</returns>
	public bool TryStartCooldown(Cooldown cooldown, MonoBehaviour owner)
	{
		if (IsOnCooldown(cooldown)) return false;
		owner.StartCoroutine(DoCooldown(cooldown));
		return true;
	}

	/// <summary>
	/// Is the indicated cooldown currently on cooldown for this player?
	/// </summary>
	/// <param name="cooldown"></param>
	/// <returns></returns>
	public bool IsOnCooldown(Cooldown cooldown)
	{
		if (cooldownTypeToCooldown.TryGetValue(cooldown, out var isOnCooldown))
		{
			return isOnCooldown;
		}

		return false;
	}

	private IEnumerator DoCooldown(Cooldown cooldown, float time)
	{
		cooldownTypeToCooldown[cooldown] = true;
		yield return WaitFor.Seconds(time);
		cooldownTypeToCooldown[cooldown] = false;
	}



}

