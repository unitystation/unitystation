using UnityEngine;


/// <summary>
/// Populates each named slot using a particular prefab, cloth, or slot populator.
/// </summary>
[CreateAssetMenu(fileName = "NamedSlotStoragePopulator", menuName = "Inventory/Populators/Storage/NamedSlotStoragePopulator", order = 2)]
public class NamedSlotStoragePopulator : ItemStoragePopulator
{
	[SerializeField]
	[Tooltip("What to use to populate each named slot")]
	[ArrayElementTitle("NamedSlot")]
	private NamedSlotPopulatorEntry[] Entries = null;

	public GameObject skirtVariant;
	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		Logger.LogTraceFormat("Populating item storage {0}", Category.Inventory, toPopulate.name);
		foreach (var entry in Entries)
		{
			var slot = toPopulate.GetNamedItemSlot(entry.NamedSlot);
			if (slot == null)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because it doesn't exist in this itemstorage {1}.",
					Category.Inventory, entry.NamedSlot, toPopulate.name);
				continue;
			}

			if (entry.Prefab == null)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because Prefab  Populator was empty for this entry.",
					Category.Inventory, entry.NamedSlot);
				continue;
			}

			if (entry.Prefab != null)
			{
				// making exception for jumpsuit/jumpskirt

				if (context.SpawnInfo.CharacterSettings.Clothing == Clothing.JumpSkirt
					&& entry.NamedSlot == NamedSlot.uniform
					&& skirtVariant != null)
				{
					var spawnskirt = Spawn.ServerPrefab(skirtVariant);
					Inventory.ServerAdd(spawnskirt.GameObject, slot);
					continue;
				}

				var spawn = Spawn.ServerPrefab(entry.Prefab);
				Inventory.ServerAdd(spawn.GameObject, slot);
			}
		}
	}
}

[System.Serializable]
public class NamedSlotPopulatorEntry
{
	[Tooltip("Named slot being populated. A NamedSlot should not appear" +
	         " more than once in these entries.")]
	public NamedSlot NamedSlot;

	[Tooltip("Prefab to spawn in this slot. Takese precedence over slot populator.")]
	public GameObject Prefab;
}

