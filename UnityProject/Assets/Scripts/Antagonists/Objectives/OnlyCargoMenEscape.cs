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

		/// <summary>
		/// Check if the nuke target was detonated
		/// </summary>
		protected override bool CheckCompletion()
		{
			foreach (Transform t in GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo.Objects.transform)
			{
				var player = t.GetComponent<PlayerScript>();
				if (player != null)
				{
					var playerDetails = PlayerList.Instance.Get(player.gameObject);
					if (playerDetails.Job != JobType.CARGOTECH)
					{
						if (playerDetails.Script.playerHealth != null && !playerDetails.Script.playerHealth.IsDead)
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