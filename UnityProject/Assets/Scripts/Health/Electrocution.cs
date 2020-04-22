using System;
using UnityEngine;

/// <summary>
/// For electrocuting a victim. NPCs not emplemented, just (human) players.
/// </summary>
public class Electrocution
{
	/// <summary>
	/// The electrocution severity.
	/// <para>None: no potential difference (no side effects)</para> 
	/// <para>Mild: exposure to voltage like that of a small battery (no consequential effects)</para>
	/// <para>Painful: imagine an electric fence (drop actively held item, minor burns)</para>
	/// <para>Lethal: high voltage line (stun, severe burns, death possible)</para>
	/// </summary>
	public enum Severity
	{
		None,
		Mild,
		Painful,
		Lethal
	}

	private const int NON_INSULATED_ITEM_RESITANCE = 20000; // 20 kilo Ohms
	// Increase the below if voltages have been tweaked and medium voltage cables now cause painful electrocutions,
	// but not so much that high voltage cables are no longer a threat (turn off power sources instead).
	private const int INSULATED_ITEM_RESISTANCE = 10000000; // 10 Mega Ohms
	private const float POWER_MODIFIER = 1f;
	private const int BURNDAMAGE_MODIFIER = 100; // Less is more.

	private Severity severity;
	private GameObject victim;
	private PlayerScript victimScript;
	private LivingHealthBehaviour victimLHB;
	private BodyPartType playerActiveHand;
	private Vector2 shockSourcePos;
	private string shockSourceName;
	private float shockPower;

	/// <summary>
	/// Finds the severity of what the electrocution would be for the given player at
	/// the given voltage.
	/// </summary>
	/// <param name="player">The player GameObject to find the severity for</param>
	/// <param name="voltage">The voltage the player would be exposed to</param>
	/// <returns>Severity enumerable - None, Mild, Painful, Lethal</returns>
	public Severity GetPlayerSeverity(GameObject player, float voltage)
	{
		float resistance = GetPlayerShockResistance(player, voltage);
		float current = voltage / resistance;
		shockPower = (current * voltage) * POWER_MODIFIER;

		// Low power (power from a small battery)
		if (shockPower >= 0.01 && shockPower < 1) severity = Severity.Mild;

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
	public Severity ElectrocuteNPC(GameObject npc, Vector2 shockSourcePos,
		string shockSourceName, float voltage)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Electrocutes a player, applying effects to the victim
	/// depending on the electrocution power.
	/// </summary>
	/// <param name="player">The player GameObject to electrocute></param>
	/// <param name="shockSourcePos">The Vector3Int position of the voltage source</param>
	/// <param name="shockSourceName">The name of the voltage source</param>
	/// <param name="voltage">The voltage the victim receives</param>
	/// <returns>Severity enumerable</returns>
	public Severity ElectrocutePlayer(GameObject player, Vector2 shockSourcePos,
		string shockSourceName, float voltage)
	{
		victim = player;
		victimLHB = player.GetComponent<LivingHealthBehaviour>();
		victimScript = player.GetComponent<PlayerScript>();
		this.shockSourcePos = shockSourcePos;
		this.shockSourceName = shockSourceName;

		if (victim.GetComponent<PlayerNetworkActions>().activeHand == NamedSlot.leftHand)
		{
			playerActiveHand = BodyPartType.LeftArm;
		}
		else
		{
			playerActiveHand = BodyPartType.RightArm;
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
	/// <param name="voltage">The potential difference across the human</param>
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
	/// <param name="player">The player to calculate shock resistance with</param>
	/// <param name="voltage">The potential difference across the player</param>
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
	/// <param name="item">The item to obtain resistance from</param>
	/// <returns>A float resistance value</returns>
	private float GetItemResistance(ItemSlot item)
	{
		// No item
		if (item.ItemObject == null) return 0;

		if (item.ItemAttributes != null && item.ItemAttributes.HasTrait(CommonTraits.Instance.Insulated))
		{
			return INSULATED_ITEM_RESISTANCE;
		}

		return NON_INSULATED_ITEM_RESITANCE;
	}

	/// <summary>
	/// Applies burn damage to the specified victim's bodyparts.
	/// Attack type is internal, so as to avoid adding electrical resistances to Armor class.
	/// </summary>
	/// <param name="damage">The amount of damage to apply to the bodypart</param>
	/// <param name="bodypart">The BodyPartType to damage.</param>
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
		SoundManager.PlayNetworkedAtPos("Sparks#", shockSourcePos);
		Inventory.ServerDrop(victimScript.ItemStorage.GetActiveHandSlot());
		// Slip is essentially a yelp SFX.
		SoundManager.PlayNetworkedAtPos("Slip", victim.RegisterTile().WorldPosition,
			UnityEngine.Random.Range(0.4f, 1f), sourceObj: victim);
		Chat.AddExamineMsgFromServer(victim, $"The {shockSourceName} gives you a small electric shock!");

		DealDamage(5, playerActiveHand);
	}

	private void PlayerLethalElectrocution()
	{
		// TODO: Add sparks VFX at shockSourcePos.
		// TODO: Implement electrocution animation
		// TODO: Consider adding a scream SFX.
		SoundManager.PlayNetworkedAtPos("Sparks#", shockSourcePos);
		victim.GetComponent<RegisterPlayer>().ServerStun();
		SoundManager.PlayNetworkedAtPos("Bodyfall", victim.RegisterTile().WorldPosition, sourceObj: victim);
		// Consider removing this message when the shock animation has been implemented as it should be obvious enough.
		Chat.AddExamineMsgFromServer(victim, $"The {shockSourceName} electrocutes you!");

		var damage = shockPower / BURNDAMAGE_MODIFIER;
		DealDamage(damage * 0.4f, playerActiveHand);
		DealDamage(damage * 0.25f, BodyPartType.Chest);
		DealDamage(damage * 0.175f, BodyPartType.LeftLeg);
		DealDamage(damage * 0.175f, BodyPartType.RightLeg);
	}
}
