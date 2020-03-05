using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// Make sure at least 50% of all cargomen are alive by round end
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/CargoMenSurvival")]
	public class CargoMenSurvival : Objective
	{
		protected override void Setup()
		{
		}

		/// <summary>
		/// Check if required amount of cargo techs are alive
		/// </summary>
		protected override bool CheckCompletion()
		{
			int allCargomen = 0;
			int allAliveCargomen = 0;
			foreach (var p in PlayerList.Instance.AllPlayers)
			{
				if (p.Script == null) continue;
				if (p.Job == JobType.CARGOTECH || p.Job == JobType.QUARTERMASTER
				                               || p.Job == JobType.MINER)
				{
					allCargomen++;
				}

				if (p.Script.playerHealth != null)
				{
					if (!p.Script.playerHealth.IsDead)
					{
						allAliveCargomen++;
					}
				}
			}

			if (allAliveCargomen >= (double)allCargomen / 2)
			{
				return true;
			}

			return false;
		}
	}
}