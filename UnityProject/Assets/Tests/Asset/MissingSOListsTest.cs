using NUnit.Framework;
using ScriptableObjects.Systems.Spells;

namespace Tests.Asset
{
	public class MissingSOListsTest
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

		[Test]
		public void AlertSOsListHasAllAlertSOs()
		{
			var report = new TestReport();

			var AllSpells = Utils.FindAssetsByType<AlertSO>();

			foreach (var AlertSO in AllSpells)
			{
				report.FailIfNot( AlertSOs.Instance.AllAlertSOs.Contains(AlertSO)).AppendLine($"AlertSO {AlertSO} is missing From the AlertSOs AlertSO SO , Please add in ");
			}

			report.AssertPassed();
		}
	}
}
