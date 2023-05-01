
using UnityEngine;

/// <summary>
/// ItemStoragePopulator which simply chains together multiple ItemStoragePopulators
/// </summary>
[CreateAssetMenu(fileName = "StoragePopulatorChain", menuName = "Inventory/Populators/Storage/StoragePopulatorChain", order = 3)]
public class StoragePopulatorChain : ItemStoragePopulator
{
	[SerializeField]
	[Tooltip("Sequence of populators to use. They are executed in order, so if multiple populators populate the same slot," +
	         " the first one will take precedence.")]
	private ItemStoragePopulator[] Populators = null;

	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context, SpawnInfo info)
	{
		foreach (var populator in Populators)
		{
			populator.PopulateItemStorage(toPopulate, context, info);
		}
	}
}
