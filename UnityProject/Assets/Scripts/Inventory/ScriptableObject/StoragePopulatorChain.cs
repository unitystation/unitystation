
using UnityEngine;

/// <summary>
/// ItemStoragePopulator which simply chains together multiple ItemStoragePopulators
/// </summary>
public class StoragePopulatorChain : ItemStoragePopulator
{
	[Tooltip("Sequence of populators to use.")]
	public ItemStoragePopulator[] Populators;
	public override void PopulateItemStorage(ItemStorage toPopulate)
	{
		foreach (var populator in Populators)
		{
			populator.PopulateItemStorage(toPopulate);
		}
	}
}
