using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	/// <summary>
	/// An objective to set off the nuke on the station
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/OnlyCargoMenEscape")]
	public class OnlyCargoMenEscape : Objective
	{
		protected override void Setup()
		{
		}

		protected override bool CheckCompletion()
		{
			int playersFound = 0;
			foreach (Transform t in GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo.Objects.transform)
			{
				var player = t.GetComponent<PlayerScript>();
				if (player != null)
				{
					playersFound++;
					var playerDetails = PlayerList.Instance.Get(player.gameObject);
					if (playerDetails.Job != JobType.CARGOTECH && playerDetails.Job != JobType.MINER
					                                           && playerDetails.Job != JobType.QUARTERMASTER)
					{
						if(playerDetails.Script == null || playerDetails.Script.playerHealth == null) continue;
						if (!playerDetails.Script.playerHealth.IsDead)
						{
							return false;
						}
					}
				}
			}

			if (playersFound != 0)
			{
				return true;
			}

			return false;
		}
	}
}