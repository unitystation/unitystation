using System;
using System.Collections.Generic;
using UnityEngine;

public class Electrocution
{
	public enum Severity
    {
		None,
		Mild,
		Painful,
		Lethal
    }

	private Severity severity;
	private GameObject player;
	private Vector3Int shockSourcePos;
	private string shockSourceName;
	private float shockPower;

	/// <summary>
	/// Electrocutes a player.
	/// </summary>
	/// <param name="player">The player to electrocute.</param>
	/// <param name="targetCell">The location to spawn sparks (e.g. from interacting with an electrified grille).</param>
	/// <param name="voltage">The voltage of the electrocution.</param>
	/// <returns></returns>
	public Severity ElectrocutePlayer(GameObject player, Vector3Int shockSourcePos,
		string shockSourceName, float voltage)
    {
		this.player = player;
		this.shockSourcePos = shockSourcePos;
		this.shockSourceName = shockSourceName;

		switch(GetSeverity(voltage))
        {
			case Severity.None:
				break;
			case Severity.Mild:
				PlayerMildElectrocution();
				break;
			case Severity.Painful:
				PlayerPainfulElectrocution();
				break;
			case Severity.Lethal:
				PlayerLethalElectrocution();
				break;
        }

		return severity;
    }

	/// <summary>
    /// Not implemented
    /// </summary>
    /// <param name="npc"></param>
    /// <returns></returns>
	public Severity ElectrocuteNPC(GameObject npc)
    {
		return Severity.None;
    }

	/// <summary>
	/// Applies effects to the performer depending on the electrocution power.
	/// TODO: Change interaction to performer
	/// TODO: Tweak thresholds
	/// </summary>
	/// <param name="interaction"></param>
	/// <param name="supplyVoltage"></param>
	public Severity GetSeverity(float voltage)
	{
		float resistance = GetPlayerShockResistance(player, voltage);
		float current = voltage / resistance;
		shockPower = current * voltage;

		// TODO: REMOVE
		Chat.AddSystemMsgToChat("Power: " + shockPower.ToString(), MatrixManager.MainStationMatrix);

		// Low power (power from a small battery or no power at all)
		if (shockPower >= 0.001 && shockPower < 1) return Severity.Mild;

		// Medium power (imagine an electric fence)
		else if (shockPower >= 1 && shockPower < 1000) return Severity.Painful;

		// High power (shorting a power line)
		else if (shockPower >= 1000) return Severity.Lethal;

		else return Severity.None;
	}

	/// <summary>
	 /// Calculates the human body's hand-to-foot electrical resistance based on the voltage.
	 /// Based on the figures provided by Wikipedia's electrical injury page (hand-to-hand).
	 /// Trends to 1200 Ohms at significant voltages.
	 /// </summary>
	 /// <param name="voltage"></param>
	 /// <returns></returns>
	private float GetHumanHandFeetResistance(float voltage)
	{
		float resistance = 1000 + (3000 / (1 + (float)Math.Pow(voltage / 55, 1.5f)));
		return resistance *= 1.2f; // Path hand-to-foot is slightly longer than hand-to-hand. BUT TWO FEET
	}

	/// <summary>
	/// Calculates the performer's total resistance using a base resistance value,
	/// their health and the items the performer is wearing or holding.
	/// </summary>
	/// <param name="performer"></param>
	/// <param name="voltage"></param>
	/// <returns></returns>
	private float GetPlayerShockResistance(GameObject player, float voltage)
	{
		// TODO: Check if floating (no gravity) or otherwise not touching metal floor plating (or some other metal structure)

		// Assume the player is a human
		float resistance = GetHumanHandFeetResistance(voltage);

		// Give the player extra/less electrical resistance based on what they're holding/wearing
		List<NamedSlot> bodySlots = new List<NamedSlot>() {
			NamedSlot.hands, NamedSlot.feet, NamedSlot.uniform, NamedSlot.outerwear, NamedSlot.handcuffs };
		foreach (var slot in bodySlots)
		{
			var item = player.GetComponent<PlayerScript>().ItemStorage.GetNamedItemSlot(slot);
			if (item != null)
			{
				if (item.ItemAttributes != null && item.ItemAttributes.HasTrait(CommonTraits.Instance.Insulated))
				{
					resistance += 100000;
				}
				// TODO: If handcuffs, halve in the arms!
				// TODO: GetActiveHandItem() and increase/reduce resistance.
				// TODO: Check resistances for hardsuits, uniforms etc.
				else
				{
					resistance += 1000;
				}
			}
		}

		// TODO: check for broken skin (brute, burn) to reduce resistance or even missing limb.
		// If both legs missing, they must be on the ground or in wheelchair, calculate resistance as appropriate

		return resistance;
	}

	private void PlayerMildElectrocution()
    {
		Chat.AddExamineMsgFromServer(player, $"The {shockSourceName} gives you a slight tingling sensation...");
	}

	private void PlayerPainfulElectrocution()
    {
		// TODO: Consider adding a yelp SFX.
		// TODO: Chance a small amount of damage based on power
		SoundManager.PlayAtPosition("Sparks#", shockSourcePos, player); // Review the args are correct (see the soundmanager function)
		Inventory.ServerDrop(player.GetComponent<PlayerScript>().ItemStorage.GetActiveHandSlot());
		Chat.AddExamineMsgFromServer(player, $"The {shockSourceName} gives you a small electric shock!");
	}

	private void PlayerLethalElectrocution()
    {
		// TODO: Implement electrocution animation
		// TODO: Add burn damage to performer
		// TODO: Consider adding a scream SFX.
		SoundManager.PlayAtPosition("Sparks#", shockSourcePos, player);
		player.GetComponent<RegisterPlayer>().ServerStun();
		SoundManager.PlayAtPosition("Bodyfall", player.transform.position, player);
		// Consider removing this message when the shock animation has been implemented as it should be obvious enough.
		Chat.AddExamineMsgFromServer(player, $"The {shockSourceName} electrocutes you!");
	}
}
