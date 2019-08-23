
/// <summary>
/// enum-like class representing a hand slot. An immutable class with constant values for each slot. It's better to use this instead of
/// strings.
/// </summary>
public class HandSlot
{
	public static readonly HandSlot RIGHT = new HandSlot(EquipSlot.rightHand);
	public static readonly HandSlot LEFT = new HandSlot(EquipSlot.leftHand);

	public EquipSlot equipSlot { get; private set; }

	private HandSlot(EquipSlot newEquipSlot)
	{
		equipSlot = newEquipSlot;
	}

	/// <summary>
	/// Gets the hand slot with the specified name.
	/// </summary>
	/// <param name="slotName">leftHand or rightHand</param>
	/// <returns></returns>
	public static HandSlot ForName(EquipSlot staticEquipSlot)
	{
		if (staticEquipSlot == EquipSlot.rightHand)
		{
			return RIGHT;
		}
		else if (staticEquipSlot == EquipSlot.leftHand)
		{
			return LEFT;
		}
		else
		{
			Logger.LogErrorFormat("{0} is not a valid hand slot name, should be leftHand or rightHand.", Category.UI, "equipSlot");
			return null;
		}
	}

}
