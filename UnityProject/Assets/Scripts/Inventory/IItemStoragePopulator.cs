/// <summary>
/// Defines how an ItemStorage should be populated with stuff.
/// </summary>
public interface IItemStoragePopulator
{
	/// <summary>
	/// Populate the specified item storage with stuff.
	/// </summary>
	/// <param name="toPopulate">storage to populate</param>
	void PopulateItemStorage(ItemStorage toPopulate);
}
