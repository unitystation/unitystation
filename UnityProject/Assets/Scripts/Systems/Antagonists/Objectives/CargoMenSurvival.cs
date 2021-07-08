using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// Make sure at least 50% of all cargomen are alive by round end
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/CargoMenSurvival")]
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
			int allRebels = 0;
			int allAliveRebels = 0;
			foreach (var p in PlayerList.Instance.AllPlayers)
			{
				if (p.Script == null) continue;

				foreach (JobType rebeljob in GameManager.Instance.Rebels)
				{
					if (p.Job == rebeljob)
					{
						allRebels++;
						if (p.Script.playerHealth != null && !p.Script.playerHealth.IsDead)
						{
							allAliveRebels++;
						}
					}
				}
			}

			if (allAliveRebels >= (double)allRebels / 2)
			{
				return true;
			}

			return false;
		}
	}
}