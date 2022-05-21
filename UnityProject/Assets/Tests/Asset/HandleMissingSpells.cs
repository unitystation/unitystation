using NUnit.Framework;
using ScriptableObjects.Systems.Spells;

namespace Tests.Asset
{
	[Category(nameof(Asset))]
	public class HandleMissingSpells
	{
		[Test]
		public void SpellDataIsNotMissingImplementation()
		{
			var report = new TestReport();
			var spellImplName = $"{nameof(SpellData)}.{nameof(SpellData.SpellImplementation)}";

			foreach (var spellData in Utils.FindAssetsByType<SpellData>())
			{
				if (spellData.SpellImplementation != null) continue;
				report.Fail()
					.AppendLine(
						$"{spellData.Name} is missing a {spellImplName}, a default implementation will be added.");
				spellData.CheckImplementation();
			}

			report.AppendLine("Please commit spell changes.");
			report.AssertPassed();
		}
	}
}
