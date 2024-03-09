using System.Linq;
using NUnit.Framework;
using ScriptableObjects.Systems.Spells;
using UnityEngine;

namespace Tests.Asset
{
	[Category(nameof(Asset))]
	public class HandleMissingSpells
	{
		public void SpellDataIsNotMissingImplementation()
		{
			var report = new TestReport();
			var spellImplName = $"{nameof(SpellData)}.{nameof(SpellData.SpellImplementation)}";

			Debug.Log("[SpellData] Check if all spells have a {spellImplName} implementation.");

			var spells = Utils.FindAssetsByType<SpellData>().ToList();
			if (spells.Count == 0)
			{
				Debug.Log("piss");
			}

			foreach (var spellData in spells)
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
