using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Storage
{
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

		public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context, SpawnInfo info)
		{
			Loggy.LogError("This shouldn't be used but  is required for inheritance", Category.EntitySpawn);
		}

		public virtual void PopulateDynamicItemStorage(DynamicItemStorage toPopulate, PlayerScript PlayerScript, bool useStandardPopulator = true)
		{
			if (useStandardPopulator && toPopulate.StandardPopulator != this)
			{
				toPopulate.StandardPopulator.PopulateDynamicItemStorage(toPopulate, PlayerScript);
			}

			Entries = Entries.OrderBy(entry => entry.NamedSlot).ToList();

			Loggy.LogTraceFormat("Populating item storage {0}", Category.EntitySpawn, toPopulate.name);
			foreach (var entry in Entries)
			{
				var slots = toPopulate.GetNamedItemSlots(entry.NamedSlot);
				if (slots.Count == 0)
				{
					Loggy.LogTraceFormat("Skipping populating slot {0} because it doesn't exist in this itemstorage {1}.",
						Category.EntitySpawn, entry.NamedSlot, toPopulate.name);
					continue;
				}

				if (entry.Prefab == null)
				{
					Loggy.LogTraceFormat("Skipping populating slot {0} because Prefab  Populator was empty for this entry.",
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
							spawnskirt.GameObject.GetComponent<ItemStorage>()?.SetRegisterPlayer(PlayerScript.RegisterPlayer);
							Inventory.ServerAdd(spawnskirt.GameObject, slot, entry.ReplacementStrategy, true);
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
									spawnThing = (duffelVariant != null) ? duffelVariant : entry.Prefab;
									break;

								case BagStyle.Satchel:
									spawnThing = (satchelVariant != null) ? satchelVariant : entry.Prefab;
									break;
								default:
									spawnThing = entry.Prefab;
									break;
							}

							var spawnbackpack = Spawn.ServerPrefab(spawnThing, PrePickRandom: true);
							spawnbackpack.GameObject.GetComponent<ItemStorage>()?.SetRegisterPlayer(PlayerScript.RegisterPlayer);
							Inventory.ServerAdd(spawnbackpack.GameObject, slot, entry.ReplacementStrategy, true);
							PopulateSubInventory(spawnbackpack.GameObject, entry.namedSlotPopulatorEntrys);
							break;
						}
						var spawn = Spawn.ServerPrefab(entry.Prefab, PrePickRandom: true);
						spawn.GameObject.GetComponent<ItemStorage>()?.SetRegisterPlayer(PlayerScript.RegisterPlayer);
						Inventory.ServerAdd(spawn.GameObject, slot, entry.ReplacementStrategy, true);
						PopulateSubInventory(spawn.GameObject, entry.namedSlotPopulatorEntrys);
						break;
					}
				}
			}
		}

		public void PopulateSubInventory(GameObject gameObject, List<SlotPopulatorEntryRecursive> namedSlotPopulatorEntrys)
		{
			if (namedSlotPopulatorEntrys.Count == 0) return;

			var ItemStorage = gameObject.GetComponent<ItemStorage>();
			if (ItemStorage == null) return;

			foreach (var namedSlotPopulatorEntry in namedSlotPopulatorEntrys)
			{
				if (namedSlotPopulatorEntry == null || namedSlotPopulatorEntry.Prefab == null) continue;
				ItemSlot ItemSlot;

				if (namedSlotPopulatorEntry.DoNotGetFirstEmptySlot == false)
				{
					ItemSlot =  ItemStorage.GetNextEmptySlot();
				}
				else
				{
					if (namedSlotPopulatorEntry.UseIndex)
					{
						ItemSlot = ItemStorage.GetIndexedItemSlot(namedSlotPopulatorEntry.IndexSlot);
					}
					else
					{
						ItemSlot = ItemStorage.GetNamedItemSlot(namedSlotPopulatorEntry.NamedSlot);
					}

					if (ItemSlot.Item != null && namedSlotPopulatorEntry.IfOccupiedFindEmptySlot)
					{
						ItemSlot = ItemStorage.GetNextFreeIndexedSlot();
					}
				}

				if (ItemSlot == null) continue;
				var spawn = Spawn.ServerPrefab(namedSlotPopulatorEntry.Prefab, PrePickRandom: true);

				if (Validations.CanFit(ItemSlot, spawn.GameObject, NetworkSide.Server) == false)
				{
					Loggy.LogError($"Your initial contents spawn for Storage {gameObject.name} for {spawn.GameObject} Is bypassing the Can fit requirements");
				}

				Inventory.ServerAdd(spawn.GameObject, ItemSlot, namedSlotPopulatorEntry.ReplacementStrategy, true);
			}
		}
	}

	/// <summary>
	/// Used for populating a specified index lot or inventory slot, then can specify what should be populated in the inventory of what was populated in the inventory Slot that was specified
	/// </summary>
	[Serializable]
	public class SlotPopulatorEntry : SlotPopulatorEntryRecursive
	{
		public List<SlotPopulatorEntryRecursive> namedSlotPopulatorEntrys = new List<SlotPopulatorEntryRecursive>();
	}

	/// <summary>
	/// Used for populating a specified index lot or inventory slot, then can specify what should be populated in the inventory of what was populated in the inventory Slot that was specified
	/// </summary>
	[System.Serializable]
	public class SlotPopulatorEntryRecursive
	{
		public bool DoNotGetFirstEmptySlot = false;

		[Tooltip("Prefab to spawn in this slot. Takes precedence over slot populator.")]
		public GameObject Prefab;

		[HorizontalLine]

		[FormerlySerializedAs("UesIndex")] [Tooltip(" Place object in Specified indexed slot or Use named slot Identifer ")]
		public bool UseIndex = false;

		[Tooltip("  The Index lot that the prefab will be spawned into " )]
		public int IndexSlot = 0;

		[Tooltip("Named slot being populated. A NamedSlot should not appear" +
		                                         " more than once in these entries.")]
		public NamedSlot NamedSlot = NamedSlot.none;

		[HorizontalLine]

		public bool IfOccupiedFindEmptySlot = true;


		public ReplacementStrategy ReplacementStrategy = ReplacementStrategy.DropOther;
	}
}
