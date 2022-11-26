using NUnit.Framework;
using ScriptableObjects.Systems.Spells;

namespace Tests.Asset
{
	public class SpellListTest
	{
		[Test]
		public void SpellListHasAllSpells()
		{
			var report = new TestReport();

			var AllSpells = Utils.FindAssetsByType<SpellData>();


			foreach (var Spell in AllSpells)
			{
				report.FailIfNot(SpellList.Instance.Spells.Contains(Spell)).AppendLine($"Spell {Spell} is missing From the SpellList Spells SO , Please add in ");
			}

			report.AssertPassed();
		}
	}
}
