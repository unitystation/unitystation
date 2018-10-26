using UnityEngine;

public class UISlotObject
{
	public readonly string Slot;
	public readonly GameObject SlotContents;
	public readonly string FromSlot;

	public UISlotObject(string slot, GameObject slotContents = null, string fromSlot = "")
	{
		Slot = slot;
		SlotContents = slotContents;
		FromSlot = fromSlot;
	}

	public bool IsEmpty()
	{
		return !SlotContents;
	}

	public override string ToString()
	{
		return string.Format("UISlotObject {0}: {1}", Slot, SlotContents);
	}
}