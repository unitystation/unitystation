
using System.Collections.Generic;

/// <summary>
/// Uniquely identifies a slot within a particular ItemStorage. Does NOT uniquely identify a specific ItemSlot
/// within an instance of the game - it is only unique within a particular ItemStorage.
///
/// A slot can be identified by name if it's a named slot or by an index if it's an indexed slot.
///
/// Uses object pooling so there's only ever one instance of a slot identifier.
/// </summary>
public class SlotIdentifier
{
	//object pool since there's no need for duplicate identifiers.
	private static Dictionary<NamedSlot,SlotIdentifier> namedIdentifiers = new Dictionary<NamedSlot, SlotIdentifier>();
	private static Dictionary<int,SlotIdentifier> indexedIdentifiers = new Dictionary<int, SlotIdentifier>();

	/// <summary>
	/// named slot if this is a Named slot identifier, else null
	/// </summary>
	public readonly NamedSlot? NamedSlot;
	/// <summary>
	/// index of this slot if this is an indexed slot, else -1
	/// </summary>
	public readonly int SlotIndex;
	/// <summary>
	/// Type of this slot identifier
	/// </summary>
	public readonly SlotIdentifierType SlotIdentifierType;

	private SlotIdentifier(NamedSlot? namedSlot, int slotIndex, SlotIdentifierType slotIdentifierType)
	{
		NamedSlot = namedSlot;
		SlotIndex = slotIndex;
		SlotIdentifierType = slotIdentifierType;
	}

	public override string ToString()
	{
		return $"{nameof(NamedSlot)}: {NamedSlot}, {nameof(SlotIndex)}: {SlotIndex}, {nameof(SlotIdentifierType)}: {SlotIdentifierType}";
	}

	/// <summary>
	/// Gets the slot identifier for a particular named slot
	/// </summary>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public static SlotIdentifier Named(NamedSlot namedSlot)
	{
		namedIdentifiers.TryGetValue(namedSlot, out var slotID);
		if (slotID == null)
		{
			slotID = new SlotIdentifier(namedSlot, -1, SlotIdentifierType.Named);
			namedIdentifiers.Add(namedSlot, slotID);
		}

		return slotID;
	}

	/// <summary>
	/// Gets the slot identifier for an particular indexed slot.
	/// </summary>
	/// <param name="slotIndex"></param>
	/// <returns></returns>
	public static SlotIdentifier Indexed(int slotIndex)
	{
		indexedIdentifiers.TryGetValue(slotIndex, out var slotID);
		if (slotID == null)
		{
			slotID = new SlotIdentifier(null, slotIndex, SlotIdentifierType.Indexed);
			indexedIdentifiers.Add(slotIndex, slotID);
		}

		return slotID;
	}


}


public enum SlotIdentifierType
{
	/// <summary>
	/// Identified by a NamedSlot enum
	/// </summary>
	Named,
	/// <summary>
	/// Identified by an index
	/// </summary>
	Indexed
}