using System.Collections;
using System.Collections.Generic;
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

			if (AllReagents.Count != List.Count)
			{
				Assert.Fail(" ChemistryReagentsSO Is missing some reagents ");
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
