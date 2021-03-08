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
	public GameObject duffelVariant;
	public GameObject satchelVariant;
	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		Logger.LogTraceFormat("Populating item storage {0}", Category.EntitySpawn, toPopulate.name);
		foreach (var entry in Entries)
		{
			var slot = toPopulate.GetNamedItemSlot(entry.NamedSlot);
			if (slot == null)
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
				// making exception for jumpsuit/jumpskirt

				if (context.SpawnInfo.CharacterSettings.ClothingStyle == ClothingStyle.JumpSkirt
					&& entry.NamedSlot == NamedSlot.uniform
					&& skirtVariant != null)
				{
					var spawnskirt = Spawn.ServerPrefab(skirtVariant);
					Inventory.ServerAdd(spawnskirt.GameObject, slot);
					continue;
				}

				//exceoptions for backpack preference

				if (entry.NamedSlot == NamedSlot.back)
				{
    				///SpawnResult spawnbackpack;
   					GameObject spawnThing;

   					switch (context.SpawnInfo.CharacterSettings.BagStyle)
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

    				var spawnbackpack = Spawn.ServerPrefab(spawnThing);
    				Inventory.ServerAdd(spawnbackpack.GameObject, slot);
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

