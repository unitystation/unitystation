using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ScriptableObjects.Systems.Spells;
using Tests;
using UnityEngine;

public class SpellListTest
{
	[Test]
	public void SpellListHasAllSpells()
	{
		var report = new TestReport();

		var AllSpells = Utils.FindAssetsByType<SpellData>();


		foreach (var Spell in AllSpells)
		{
			if (SpellList.Instance.Spells.Contains(Spell) == false)
			{
				report.Fail().AppendLine($"Spell {Spell} is missing From the SpellList Spells SO , Please add in ");
			}
		}

		report.AssertPassed();
	}
}
