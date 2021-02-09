
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NOTE: You can use this (on a given object) or Cooldowns (static methods) as the main API for working with cooldowns.
///
/// Allows a game object to have cooldowns which can be triggered on, take a defined amount of time to
/// go off, and can be checked if they are on/off.
/// Intended to be used for a wide variety of things, such as interaction logic and abilities. A given cooldown
/// can be used across multiple components as well, such as for various melee-related actions that should not
/// be possible to spam all together.
///
/// Cooldowns are primarily defined using Cooldown assets, but for convenience there are also ways to use cooldowns
/// for one-off situations without needing to create an asset.
///
/// At the moment, there is currently NO networking logic for cooldowns. The clients (incl. host player) and server track cooldowns totally
/// separately. Generally clients only track cooldowns for their own local player for preventing interaction
/// messages being spammed to the server. We can add networking logic for future cooldown use cases if it becomes necessary.
/// </summary>
public class HasCooldowns : MonoBehaviour
{

	//Set which tracks which cooldowns are currently on. Note that since CooldownIdentifiers
	//explicitly identify if they are for clientside / serverside, this contains both client and
	//serverside cooldowns.
	private readonly HashSet<CooldownID> onCooldowns = new HashSet<CooldownID>();

	/// <summary>
	/// Starts the cooldown if it's not currently on.
	/// </summary>
	/// <param name="cooldown">cooldown to try</param>
	/// <param name="side">indicates which side's cooldown should be started</param>
	/// <param name="secondsOverride">custom cooldown time in seconds</param>
	/// <returns>true if cooldown was successfully started, false if cooldown was already on.</returns>
	public bool TryStart(ICooldown cooldown, NetworkSide side, float secondsOverride = float.NaN)
	{
		return TryStart(CooldownID.Asset(cooldown, side), float.IsNaN(secondsOverride) ? cooldown.DefaultTime : secondsOverride);
	}

	/// <summary>
	/// Starts a cooldown (if it's not already on) which is identified by a particular TYPE of interactable component. The specific instance
	/// doesn't matter - this cooldown is shared by all instances of that type. For example, you'd always start
	/// the same cooldown regardless of which object's Meleeable component you passed to this (this is
	/// usually the intended behavior for interactable components - you don't care which object's interaction
	/// you are triggering - you should still have the same cooldown for melee regardless of who you are hitting).
	/// Intended for convenience / one-off usage in small interactable components so you don't need
	/// to create an asset.
	/// </summary>
	/// <param name="cooldown">interactable component whose cooldown should be started</param>
	/// <param name="seconds">how many seconds the cooldown should take</param>
	/// <param name="side">indicates which side's cooldown should be started</param>
	/// <returns>true if cooldown was successfully started, false if cooldown was already on.</returns>
	public bool TryStart<T>(IInteractable<T> interactable, float seconds, NetworkSide side)
		where T: Interaction
	{
		return TryStart(CooldownID.Interaction(interactable, side), seconds);
	}

	/// <summary>
	/// Checks if the indicated cooldown is on (currently counting down) for the indicated side.
	/// </summary>
	/// <param name="cooldownId"></param>
	/// <param name="side"></param>
	/// <returns></returns>
	public bool IsOn(CooldownID cooldownId)
	{
		return onCooldowns.Contains(cooldownId);
	}

	/// <summary>
	/// Checks if the indicated cooldown is off (done counting down) for the indicated side.
	/// </summary>
	/// <param name="cooldownId"></param>
	/// <param name="side"></param>
	/// <returns></returns>
	public bool IsOff(CooldownID cooldownId)
	{
		return !IsOn(cooldownId);
	}

	private bool TryStart(CooldownID cooldownId, float seconds)
	{
		if (onCooldowns.Contains(cooldownId))
		{
			//already on, don't start it
			return false;
		}

		//go ahead and start it
		StartCoroutine(DoCooldown(cooldownId, seconds));
		return true;
	}

	private IEnumerator DoCooldown(CooldownID cooldownId, float seconds)
	{
		Logger.LogTraceFormat("Started cooldown {0}: {1} seconds", Category.Cooldowns, cooldownId, seconds);
		onCooldowns.Add(cooldownId);
		yield return WaitFor.Seconds(seconds);
		onCooldowns.Remove(cooldownId);
		Logger.LogTraceFormat("Finished cooldown {0}: {1} seconds", Category.Cooldowns, cooldownId, seconds);
	}
}
