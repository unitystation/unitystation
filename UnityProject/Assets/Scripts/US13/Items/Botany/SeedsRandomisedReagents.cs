using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Utils;
using US13.Items.Botany;
using US13.ScriptableObjects;
using US13.Systems.Botany;
using US13.Systems.ChemistryInMainAssembly;
using Util;

namespace Seeds
{
	public class SeedsRandomisedReagents : MonoBehaviour
	{
		public float ChanceForAdditionalChemical = 0.12f;

		public float ChemicalRangeMin = 0.04f;
		public float ChemicalRangeMax = 0.33f;
		public void Start()
		{
			var SP = GetComponent<SeedPacket>();
			SP.plantData.ReagentProduction.Add(new ReagentNPercentage()
			{
				ChemistryReagent = ChemistryReagentsSO.Instance.AllChemistryReagents.PickRandom(),
				percentage = (float) Math.Round(RNG.GetRandomNumber(ChemicalRangeMin, ChemicalRangeMax), 2)
			});

			if (RNG.RoleChance(ChanceForAdditionalChemical))
			{
				SP.plantData.ReagentProduction.Add(new ReagentNPercentage()
				{
					ChemistryReagent = ChemistryReagentsSO.Instance.AllChemistryReagents.PickRandom(),
					percentage =(float) Math.Round(RNG.GetRandomNumber(ChemicalRangeMin, ChemicalRangeMax), 2)
				});
			}
		}
	}

}
