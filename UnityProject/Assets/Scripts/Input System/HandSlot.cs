
/// <summary>
/// enum-like class representing a hand slot. An immutable class with constant values for each slot. It's better to use this instead of
/// strings.
/// </summary>
public class HandSlot
{
	public static readonly HandSlot RIGHT = new HandSlot("rightHand");
	public static readonly HandSlot LEFT = new HandSlot("leftHand");

	private string slotName;

	/// <summary>
	/// The inventory slot name of this hand.
	/// </summary>
	public string SlotName => slotName;

	private HandSlot(string slotName) => this.slotName = slotName;

	/// <summary>
	/// Gets the hand slot with the specified name.
	/// </summary>
	/// <param name="slotName">leftHand or rightHand</param>
	/// <returns></returns>
	public static HandSlot ForName(string slotName)
	{
		if (slotName == "rightHand")
		{
			return RIGHT;
		}
		else if (slotName == "leftHand")
		{
			return LEFT;
		}
		else
		{
			Logger.LogErrorFormat("{0} is not a valid hand slot name, should be leftHand or rightHand.", Category.UI, slotName);
			return null;
		}
	}

}
