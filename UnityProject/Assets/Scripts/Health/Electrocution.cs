using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For electrocuting a victim. NPCs not emplemented, just (human) players.
/// Might be a good idea to just receive a GameObject instead of requestiong both
/// the shock origin's position and name separately.
/// </summary>
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
	private GameObject victim;
	private PlayerScript victimScript;
	private LivingHealthBehaviour victimLHB;
	private BodyPartType playerActiveHand;
	private Vector3Int shockSourcePos;
	private string shockSourceName;
	private float shockPower;

	/// <summary>
	/// Finds the severity of what the electrocution would be for the given player at
	/// the given voltage.
	/// </summary>
	/// <param name="player">GameObject player</param>
	/// <param name="voltage">float voltage</param>
	/// <returns>Severity enumerable</returns>
	public Severity GetPlayerSeverity(GameObject player, float voltage)
	{
		float resistance = GetPlayerShockResistance(player, voltage);
		float current = voltage / resistance;
		shockPower = current * voltage;

		// Low power (power from a small battery)
		if (shockPower >= 0.001 && shockPower < 1) severity = Severity.Mild;

		// Medium power (imagine an electric fence)
		else if (shockPower >= 1 && shockPower < 100) severity = Severity.Painful;

		// High power (shorting a power line)
		else if (shockPower >= 100) severity = Severity.Lethal;

		else severity = Severity.None;

		return severity;
	}

	/// <summary>
	/// Not implemented
	/// </summary>
	/// <returns>Severity enumerable</returns>
	public Severity ElectrocuteNPC(GameObject npc, Vector3Int shockSourcePos,
		string shockSourceName, float voltage)
	{
		return Severity.None;
	}

	/// <summary>
	/// Electrocutes a player, applying effects to the victim
    /// depending on the electrocution power.
	/// </summary>
	/// <param name="player"GameObject player></param>
	/// <param name="shockSourcePos">Vector3Int shockSourcePos</param>
	/// <param name="shockSourceName">string shockSourceName</param>
	/// <param name="voltage">float voltage</param>
	/// <returns>Severity enumerable</returns>
	public Severity ElectrocutePlayer(GameObject player, Vector3Int shockSourcePos,
		string shockSourceName, float voltage)
    {
		this.victim = player;
		this.victimLHB = player.GetComponent<LivingHealthBehaviour>();
		this.victimScript = player.GetComponent<PlayerScript>();
		this.shockSourcePos = shockSourcePos;
		this.shockSourceName = shockSourceName;

		if (victim.GetComponent<PlayerNetworkActions>().activeHand == NamedSlot.leftHand)
		{
			this.playerActiveHand = BodyPartType.LeftArm;
		}
		else
		{
			this.playerActiveHand = BodyPartType.RightArm;
        }

		switch (GetPlayerSeverity(victim, voltage))
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
	 /// Calculates the human body's hand-to-foot electrical resistance based on the voltage.
	 /// Based on the figures provided by Wikipedia's electrical injury page (hand-to-hand).
	 /// Trends to 1200 Ohms at significant voltages.
	 /// </summary>
	 /// <param name="voltage">float voltage</param>
	 /// <returns>float resistance</returns>
	private float GetHumanHandFeetResistance(float voltage)
	{
		float resistance = 1000 + (3000 / (1 + (float)Math.Pow(voltage / 55, 1.5f)));
		return resistance *= 1.2f; // A bit more resistance due to slightly longer (hand-foot) path.
	}

	/// <summary>
	/// Calculates the player's total resistance using a base human resistance value,
	/// their health and the items the performer is wearing or holding.
	/// </summary>
	/// <param name="player">GameObject player</param>
	/// <param name="voltage">float voltage</param>
	/// <returns>float resistance</returns>
	private float GetPlayerShockResistance(GameObject player, float voltage)
	{
		// Assume the player is a human
		float resistance = GetHumanHandFeetResistance(voltage);
		PlayerScript playerScript = player.GetComponent<PlayerScript>();
		LivingHealthBehaviour playerLHB = player.GetComponent<LivingHealthBehaviour>();

		// Give the player extra/less electrical resistance based on what they're holding/wearing
		resistance += GetItemResistance(playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.hands));
		resistance += GetItemResistance(playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.feet));
		if (playerScript.ItemStorage.GetActiveHandSlot().Item != null) resistance -= 300;

		// Broken skin reduces electrical resistance - arbitrarily chosen at 4 to 1.
		resistance -= 4 * playerLHB.GetTotalBruteDamage();

		// Make sure the player doesn't get ridiculous conductivity.
		if (resistance < 100) resistance = 100;
		return resistance;
	}

	/// <summary>
    /// Gets the electrical resistance of the given item.
    /// Checks if the item has the insulated trait and if so, returns with a large resistance.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
	private float GetItemResistance(ItemSlot item)
    {
		// No item
		if (item.ItemObject == null) return 0.0f;

		// Insulated item
		if (item.ItemAttributes != null && item.ItemAttributes.HasTrait(CommonTraits.Instance.Insulated))
		{
			return 100000;
		}

		// Normal item
		return 2000;
    }

	/// <summary>
    /// Applies burn damage to the specified victim's bodyparts.
    /// Attack type is internal, so as to avoid adding electrical resistances to Armor class.
    /// </summary>
    /// <param name="damage">float damage</param>
    /// <param name="bodypart">BodyPartType bodypart</param>
	private void DealDamage(float damage, BodyPartType bodypart)
    {
		victimLHB.ApplyDamageToBodypart(null, damage, AttackType.Internal, DamageType.Burn, bodypart);
    }

	private void PlayerMildElectrocution()
    {
		Chat.AddExamineMsgFromServer(victim, $"The {shockSourceName} gives you a slight tingling sensation...");
	}

	private void PlayerPainfulElectrocution()
    {
		// TODO: Add sparks VFX at shockSourcePos.
		SoundManager.PlayAtPosition("Sparks#", shockSourcePos, victim);
		Inventory.ServerDrop(victimScript.ItemStorage.GetActiveHandSlot());
		SoundManager.PlayAtPosition("Slip", victim.transform.position, victim); // Slip is essentially a yelp SFX.
		Chat.AddExamineMsgFromServer(victim, $"The {shockSourceName} gives you a small electric shock!");

		DealDamage(5, playerActiveHand);
	}

	private void PlayerLethalElectrocution()
    {
		// TODO: Add sparks VFX at shockSourcePos.
		// TODO: Implement electrocution animation
		// TODO: Consider adding a scream SFX.
		SoundManager.PlayAtPosition("Sparks#", shockSourcePos, victim);
		victim.GetComponent<RegisterPlayer>().ServerStun();
		SoundManager.PlayAtPosition("Bodyfall", victim.transform.position, victim);
		// Consider removing this message when the shock animation has been implemented as it should be obvious enough.
		Chat.AddExamineMsgFromServer(victim, $"The {shockSourceName} electrocutes you!");

		var damage = shockPower / 100; // Arbitrary
		DealDamage(damage * 0.4f, playerActiveHand);	
		DealDamage(damage * 0.125f, BodyPartType.LeftLeg);
		DealDamage(damage * 0.125f, BodyPartType.RightLeg);
		DealDamage(damage * 0.35f, BodyPartType.Chest);
	}
}
