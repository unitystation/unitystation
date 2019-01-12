using UnityEngine;

public class UISlotObject
{
	public readonly string SlotUUID;
	public readonly GameObject SlotContents;
	public readonly string FromSlotUUID;

	public UISlotObject(string slotUUID, GameObject slotContents = null, string fromSlotUUID = "")
	{
		SlotUUID = slotUUID;
		SlotContents = slotContents;
		FromSlotUUID = fromSlotUUID;
	}

	public bool IsEmpty()
	{
		return !SlotContents;
	}

	public override string ToString()
	{
		return string.Format("UISlotObject {0}: {1}: {2}", SlotUUID, SlotContents.name, FromSlotUUID);
	}
}