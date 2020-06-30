using UnityEngine;

/// <summary>
/// The rated severity for electrocution of an organic/living thing.
/// </summary>
public enum LivingShockResponse
{
	/// <summary> No potential difference (no side effects) </summary>
	None,
	/// <summary> Exposure to voltage like that of a small battery (no consequential effects) </summary>
	Mild,
	/// <summary> Exposure to high voltage - akin to an electric fence (minor burns) </summary>
	Painful,
	/// <summary> Exposure to very high voltage (stun, severe burns, death possible) </summary>
	Lethal
}

/// <summary>
/// An immutable object that contains information needed to perform an electrocution,
/// and some static methods useful to electrocution logic.
/// </summary>
public class Electrocution
{
	private const int NON_INSULATED_ITEM_RESISTANCE = 20000; // 20 kilo Ohms
	// Increase the below if voltages have been tweaked and medium voltage cables now cause painful electrocutions,
	// but not so much that high voltage cables are no longer a threat (turn off power sources instead).
	private const int INSULATED_ITEM_RESISTANCE = 100000000; // 100 Mega Ohms
	private const int BUDGETINSULATED_ITEM_RESISTANCE = 10000000; // 10 Mega Ohms

	/// <summary> The voltage determines what effects the electrocution should apply </summary>
	public readonly float Voltage;
	/// <summary> Used for spawning sparks SFX/VFX </summary>
	public readonly Vector3 ShockSourcePos;
	/// <summary> Used by notification messages - tell the player(s) what shocked them. </summary>
	public readonly string ShockSourceName;

	public Electrocution(float voltage, Vector3 shockSourcePos, string shockSourceName = null)
	{
		Voltage = voltage;
		ShockSourcePos = shockSourcePos;
		ShockSourceName = shockSourceName;
	}

	/// <summary>
	/// Calculate the power of the shock from voltage, resistance.
	/// </summary>
	/// <param name="voltage"> Electrocution source voltage </param>
	/// <param name="resistance"> Victim electrical resistance </param>
	/// <returns> Power (Watts) </returns>
	public static float CalculateShockPower(float voltage, float resistance)
	{
		return voltage * (voltage / resistance);
	}

	/// <summary>
	/// Gets the electrical resistance of the given item. Intended for checking victim's apparel.
	/// Checks if the item has the insulated trait and if so, returns with a large resistance.
	/// </summary>
	/// <param name="item">The item to obtain resistance from</param>
	/// <returns>A float resistance value</returns>
	public static float GetItemElectricalResistance(GameObject item)
	{
		if (item == null) return 0; // No item, no resistance.

		var itemAttributes = item.GetComponent<ItemAttributesV2>();
		if (itemAttributes == null) return NON_INSULATED_ITEM_RESISTANCE;

		if (itemAttributes.HasTrait(CommonTraits.Instance.Insulated))
		{
			return INSULATED_ITEM_RESISTANCE;
		}
		else if (itemAttributes.HasTrait(CommonTraits.Instance.BudgetInsulated))
		{
			return BUDGETINSULATED_ITEM_RESISTANCE;
		}

		return NON_INSULATED_ITEM_RESISTANCE;
	}
}
