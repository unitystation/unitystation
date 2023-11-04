using HealthV2;
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
				if (AlertSOs.Instance.AllAlertSOs.Contains(AlertSO) == false)
				{
					AlertSOs.Instance.FindAll();
					report.FailIfNot(false).AppendLine($"AlertSO {AlertSO} is missing From the AlertSOs AlertSO SO , Automatically adding in commit changes plz");
				}

			}

			report.AssertPassed();
		}

		[Test]
		public void SurgerySOsListHasAllSurgerySOs()
		{
			var report = new TestReport();

			var SurgeryProcedureBases = Utils.FindAssetsByType<SurgeryProcedureBase>();

			foreach (var SurgeryProcedureBase in SurgeryProcedureBases)
			{
				if (SurgeryProcedureBaseSingleton.Instance.StoredReferences.Contains(SurgeryProcedureBase) == false)
				{
					SurgeryProcedureBaseSingleton.Instance.FindAll();
					report.FailIfNot(false).AppendLine($"SurgeryProcedureBase {SurgeryProcedureBase} is missing From the StoredReferences StoredReferences SO , Automatically adding in commit changes plz");
				}
			}

			report.AssertPassed();
		}
	}
}
