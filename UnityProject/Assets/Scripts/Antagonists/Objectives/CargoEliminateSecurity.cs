using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Objectives/CargoEliminateSecurity")]
	public class CargoEliminateSecurity : Objective
	{
		protected override void Setup()
		{
		}

		protected override bool CheckCompletion()
		{
			foreach (Transform t in GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo.Objects.transform)
			{
				var player = t.GetComponent<PlayerScript>();
				if (player != null)
				{
					var playerDetails = PlayerList.Instance.Get(player.gameObject);
					if (playerDetails.Job == JobType.SECURITY_OFFICER || playerDetails.Job == JobType.HOS
					                                           || playerDetails.Job == JobType.DETECTIVE
					                                           || playerDetails.Job == JobType.WARDEN)
					{
						if(playerDetails.Script == null || playerDetails.Script.playerHealth == null) continue;
						if (!playerDetails.Script.playerHealth.IsDead)
						{
							return false;
						}
					}
				}
			}

			return true;
		}
	}
}