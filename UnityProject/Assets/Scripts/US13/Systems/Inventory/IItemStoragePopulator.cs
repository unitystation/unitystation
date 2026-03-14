using System.ComponentModel;
using UnityEngine;
using US13.Core.Lifecycle;

namespace US13.Systems.Inventory
{
	/// <summary>
	/// Defines how an ItemStorage should be populated with stuff.
	/// </summary>
	public interface IItemStoragePopulator
	{
		/// <summary>
		/// Populate the specified item storage with stuff.
		/// </summary>
		/// <param name="toPopulate">storage to populate</param>
		/// <param name="populationContext">details / context of the population being performed.</param>
		void PopulateItemStorage(IStoreThings toPopulate,MonoBehaviour component, PopulationContext populationContext, SpawnInfo info);
	}
}
