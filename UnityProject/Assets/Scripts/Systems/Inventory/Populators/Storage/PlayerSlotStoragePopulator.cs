using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Populates each named slot using a particular prefab, cloth, or slot populator.
/// </summary>
[CreateAssetMenu(fileName = "NamedSlotStoragePopulator", menuName = "Inventory/Populators/Storage/NamedSlotStoragePopulator", order = 2)]
public class PlayerSlotStoragePopulator : ItemStoragePopulator
{
	[SerializeField]
	[Tooltip("What to use to populate each named slot")]
	[ArrayElementTitle("NamedSlot")]
	public List<SlotPopulatorEntry> Entries = new List<SlotPopulatorEntry>();

	public GameObject skirtVariant;
	public GameObject duffelVariant;
	public GameObject satchelVariant;
	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		Logger.LogError("This shouldn't be used but  is required for inheritance", Category.EntitySpawn);
	}

	public virtual void PopulateDynamicItemStorage(DynamicItemStorage toPopulate, PlayerScript PlayerScript)
	{
		if (toPopulate.StandardPopulator != this) toPopulate.StandardPopulator.PopulateDynamicItemStorage(toPopulate,PlayerScript);
		Entries = Entries.OrderBy(entry => entry.NamedSlot).ToList();

		Logger.LogTraceFormat("Populating item storage {0}", Category.EntitySpawn, toPopulate.name);
		foreach (var entry in Entries)
		{
			var slots = toPopulate.GetNamedItemSlots(entry.NamedSlot);
			if (slots.Count == 0)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because it doesn't exist in this itemstorage {1}.",
					Category.EntitySpawn, entry.NamedSlot, toPopulate.name);
				continue;
			}

			if (entry.Prefab == null)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because Prefab  Populator was empty for this entry.",
					Category.EntitySpawn, entry.NamedSlot);
				continue;
			}

			if (entry.Prefab != null)
			{
				foreach (var slot in slots)
				{

					// making exception for jumpsuit/jumpskirt
					if (toPopulate.registerPlayer.PlayerScript.characterSettings.ClothingStyle == ClothingStyle.JumpSkirt
					    && entry.NamedSlot == NamedSlot.uniform
					    && skirtVariant != null)
					{
						var spawnskirt = Spawn.ServerPrefab(skirtVariant, PrePickRandom: true);
						Inventory.ServerAdd(spawnskirt.GameObject, slot, entry.ReplacementStrategy, true );
						spawnskirt.GameObject.GetComponent<ItemStorage>()?.SetRegisterPlayer(PlayerScript.registerTile);
						PopulateSubInventory(spawnskirt.GameObject, entry.namedSlotPopulatorEntrys);
						break;
					}

					//exceoptions for backpack preference

					if (entry.NamedSlot == NamedSlot.back)
					{
						///SpawnResult spawnbackpack;
						GameObject spawnThing;

						switch (toPopulate.registerPlayer.PlayerScript.characterSettings.BagStyle)
						{
							case BagStyle.Duffle:
								spawnThing = (duffelVariant != null) ? duffelVariant: entry.Prefab;
								break;

							case BagStyle.Satchel:
								spawnThing = (satchelVariant != null) ? satchelVariant: entry.Prefab;
								break;
							default:
								spawnThing = entry.Prefab;
								break;
						}

						var spawnbackpack = Spawn.ServerPrefab(spawnThing, PrePickRandom: true);
						Inventory.ServerAdd(spawnbackpack.GameObject, slot, entry.ReplacementStrategy, true );
						spawnbackpack.GameObject.GetComponent<ItemStorage>()?.SetRegisterPlayer(PlayerScript.registerTile);
						PopulateSubInventory(spawnbackpack.GameObject, entry.namedSlotPopulatorEntrys);
						break;
					}
					var spawn = Spawn.ServerPrefab(entry.Prefab, PrePickRandom: true);
					Inventory.ServerAdd(spawn.GameObject, slot, entry.ReplacementStrategy, true );
					spawn.GameObject.GetComponent<ItemStorage>()?.SetRegisterPlayer(PlayerScript.registerTile);
					PopulateSubInventory(spawn.GameObject, entry.namedSlotPopulatorEntrys);
					break;
				}
			}
		}
	}

	public void PopulateSubInventory(GameObject gameObject, List<SlotPopulatorEntry> namedSlotPopulatorEntrys)
	{
		if (namedSlotPopulatorEntrys.Count == 0)  return;

		var ItemStorage = gameObject.GetComponent<ItemStorage>();
		if (ItemStorage == null) return;


		foreach (var namedSlotPopulatorEntry in namedSlotPopulatorEntrys)
		{
			ItemSlot ItemSlot;
			if (namedSlotPopulatorEntry.UesIndex)
			{
				ItemSlot = ItemStorage.GetIndexedItemSlot(namedSlotPopulatorEntry.IndexSlot);
				if (ItemSlot.Item != null && namedSlotPopulatorEntry.IfOccupiedFindEmptyIndexSlot)
				{
					ItemSlot = ItemStorage.GetNextFreeIndexedSlot();
				}
			}
			else
			{
				ItemSlot = ItemStorage.GetNamedItemSlot(namedSlotPopulatorEntry.NamedSlot);
			}
			if (ItemSlot == null) continue;

			var spawn = Spawn.ServerPrefab(namedSlotPopulatorEntry.Prefab, PrePickRandom: true);
			Inventory.ServerAdd(spawn.GameObject, ItemSlot,namedSlotPopulatorEntry.ReplacementStrategy, true );
			Inventory.PopulateSubInventory(spawn.GameObject, namedSlotPopulatorEntry.namedSlotPopulatorEntrys);
		}
	}
}

/// <summary>
/// Used for populating a specified index lot or inventory slot, then can specify what should be populated in the inventory of what was populated in the inventory Slot that was specified
/// </summary>
[System.Serializable]
public class SlotPopulatorEntry
{
	[Tooltip("Indexed only works for sub Inventory")]
	public int IndexSlot = 0;

	public bool IfOccupiedFindEmptyIndexSlot = true;

	public bool UesIndex = false;

	[Tooltip("Named slot being populated. A NamedSlot should not appear" +
	         " more than once in these entries.")]
	public NamedSlot NamedSlot = NamedSlot.none;

	[Tooltip("Prefab to spawn in this slot. Takese precedence over slot populator.")]
	public GameObject Prefab;

	public ReplacementStrategy ReplacementStrategy = ReplacementStrategy.DropOther;

	public List<SlotPopulatorEntry> namedSlotPopulatorEntrys = new List<SlotPopulatorEntry>();

}

