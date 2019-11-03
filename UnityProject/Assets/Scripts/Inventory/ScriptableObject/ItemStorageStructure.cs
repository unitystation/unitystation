
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Defines the structure of an ItemStorage - what the possible slots are.
/// </summary>
[CreateAssetMenu(fileName = "ItemStorageStructure", menuName = "Inventory/ItemStorageStructure", order = 1)]
public class ItemStorageStructure : ScriptableObject
{
	[Tooltip("Number of unnamed slots in this structure, accessible via slot index. Note that setting" +
	         "this to 0 indicates there are no indexed slots. Setting this to 1 means there is 1 indexed" +
	         " slot with index 0. Setting this to 2 means there are 2 indexed slots, with index 0 and 1 respectively.")]
	public int IndexedSlots;
	[Tooltip("Available named slots in this structure, accessible via enum. Must not contain duplicates")]
	public NamedSlot[] NamedSlots;

	[Tooltip("Capacity capabilities of this item storage.")]
	public ItemStorageCapacity Capacity;

	/// <summary>
	///
	/// </summary>
	/// <returns>identifiers of all slots this item storage provides</returns>
	public IEnumerable<SlotIdentifier> Slots()
	{
		var indexSlots = Enumerable.Range(0, IndexedSlots).Select(SlotIdentifier.Indexed);
		var namedSlots = NamedSlots == null ? Enumerable.Empty<SlotIdentifier>() : NamedSlots.Select(SlotIdentifier.Named);
		return indexSlots.Concat(namedSlots);
	}
}
