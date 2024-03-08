using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chemistry;
using NUnit.Framework;
using ScriptableObjects;
using UnityEngine;

namespace Tests.Chemistry
{
	[TestFixture]
	[Category(nameof(Chemistry))]
	public class ReferenceTests
	{
		[Test]
		public void CheckIndexOnReagents()
		{
			var List = ChemistryReagentsSO.Instance.AllChemistryReagents;
			int into = ChemistryReagentsSO.Instance.AllChemistryReagents.Count;
			for (int i = 0; i < into; i++)
			{
				if (List[i].IndexInSingleton != i)
				{
					Assert.Fail(" ChemistryReagentsSO Needs to be regenerated ");
				}
			}
		}

		[Test]
		public void CheckForMissingReagents()
		{

			var AllReagents = ChemistryReagentsSOEditor.FindAssetsByType<Reagent>();
			var List = ChemistryReagentsSO.Instance.AllChemistryReagents;

			var count = 0;
			var count2 = 0;
			var newStringBuilder = new StringBuilder();

			foreach (var reagent in List)
			{
				if (AllReagents.Contains(reagent))
				{
					count++;
					continue;
				}

				newStringBuilder.AppendLine($"{reagent?.Name} is not in ChemistryReagentsSO!");
			}

			foreach (var reagent in AllReagents)
			{
				if (List.Contains(reagent))
				{
					count2++;
					continue;
				}

				newStringBuilder.AppendLine($"{reagent?.Name} was not found by ChemistryReagentsSOEditor!");
			}

			if (List.Count != count)
			{
				Assert.Fail("ChemistryReagentsSO Is missing some reagents\n" + newStringBuilder);
			}

			if (AllReagents.Count != count2)
			{
				Assert.Fail("ChemistryReagentsSOEditor failed to find Reagents\n" + newStringBuilder);
			}
		}


		[Test]
		public void CheckForMissingReactions()
		{

			var AllReactions = ChemistryReagentsSOEditor.FindAssetsByType<Reaction>();
			var List = ChemistryReagentsSO.Instance.AllChemistryReactions;

			if (AllReactions.Count != List.Count)
			{
				Assert.Fail(" ChemistryReagentsSO Is missing some Reactions ");
			}
		}
	}
}
