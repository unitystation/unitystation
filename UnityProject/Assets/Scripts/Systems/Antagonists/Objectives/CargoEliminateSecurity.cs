using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/CargoEliminateSecurity")]
	public class CargoEliminateSecurity : Objective
	{
		protected override void Setup()
		{
		}

		protected override bool CheckCompletion()
		{
			var transform = GameManager.Instance.PrimaryEscapeShuttle.OrNull()?.MatrixInfo?.Objects.OrNull()?.transform;

			// If the primary shuttle doesn't exist in some form, should this return true?
			if (transform == null) return true;

			foreach (Transform t in transform)
			{
				var player = t.GetComponent<PlayerScript>();
				if (player != null)
				{
					var playerDetails = PlayerList.Instance.Get(player.gameObject, true);
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