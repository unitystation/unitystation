using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ScriptableObjects.Systems.Spells;
using Tests;
using UnityEngine;

public class HandleMissingSpells
{


	[Test]
	public void TestMissingSpells()
	{
		var report = new StringBuilder();
		bool Failed = false;
		var SpellDatas = Utils.FindAssetsByType<SpellData>();

		foreach (var SpellData in SpellDatas)
		{
			if (SpellData.SpellImplementation == null)
			{
				Failed = true;
				report.AppendLine($" SpellData.SpellImplementation missing on {SpellData.name}");
				SpellData.CheckImplementation();
			}
		}

		if (Failed)
		{
			report.AppendLine($" Please Commit changes ");
			Assert.Fail(report.ToString());
		}

	}

}
