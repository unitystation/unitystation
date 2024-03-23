using Items;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
	public class ComponentTests
	{
		[Test]
		public void PrefabsDoNotHaveNullItemTrait()
		{
			var report = new TestReport();

			foreach (var prefab in Utils.FindPrefabs())
			{
				report.FailIf(HasNullTrait(prefab))
					.Append($"{prefab.name} has null item trait. ")
					.Append("Remove empty index from list or add an item trait to index")
					.AppendLine();
			}

			bool HasNullTrait(GameObject prefab) =>
				prefab.TryGetComponent<ItemAttributesV2>(out var attributes)
				&& attributes.InitialTraits.Contains(null)
				&& prefab.name.Contains("Base") == false;

			report.AssertPassed();
		}
	}
}
