
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


/// <summary>
/// Defines the structure of an ItemStorage - what the possible slots are.
/// </summary>
[CreateAssetMenu(fileName = "ItemStorageStructure", menuName = "Inventory/Structure/ItemStorageStructure", order = 1)]
public class ItemStorageStructure : ScriptableObject
{
	[FormerlySerializedAs("IndexedSlots")]
	[SerializeField]
	[Tooltip("Number of unnamed slots in this structure, accessible via slot index. Note that setting" +
	         "this to 0 indicates there are no indexed slots. Setting this to 1 means there is 1 indexed" +
	         " slot with index 0. Setting this to 2 means there are 2 indexed slots, with index 0 and 1 respectively.")]
	private int indexedSlots = 0;
	public int IndexedSlots => indexedSlots;

	[SerializeField]
	[FormerlySerializedAs("NamedSlots")]
	[Tooltip("Available named slots in this structure, accessible via enum. Must not contain duplicates")]
	private NamedSlot[] namedSlots = null;
	public NamedSlot[] NamedSlots => namedSlots;


	/// <summary>
	///
	/// </summary>
	/// <returns>identifiers of all slots this item storage provides</returns>
	public IEnumerable<SlotIdentifier> Slots()
	{
		var indexSlots = Enumerable.Range(0, indexedSlots).Select(SlotIdentifier.Indexed);
		var namedSlots = this.namedSlots == null ? Enumerable.Empty<SlotIdentifier>() : this.namedSlots.Select(SlotIdentifier.Named);
		return indexSlots.Concat(namedSlots);
	}
}
