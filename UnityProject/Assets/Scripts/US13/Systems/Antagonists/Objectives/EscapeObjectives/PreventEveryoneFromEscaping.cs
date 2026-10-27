using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Managers;
using US13.Shuttles;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Systems.Antagonists.Objectives.EscapeObjectives
{
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/PreventEveryoneFromEscaping")]
	public class PreventEveryoneFromEscaping : Objective
	{
		public bool AllowAntagonists = false;

		public bool AllowPlayerThemselves = false;

		/// <summary>
		/// The shuttles that will be checked for this objective
		/// </summary>
		private List<EscapeShuttle> ValidShuttles = new List<EscapeShuttle>();

		/// <summary>
		/// Populate the list of valid escape shuttles
		/// </summary>
		protected override void Setup()
		{
			ValidShuttles.Add(GameManager.Instance.PrimaryEscapeShuttle);
		}

		/// <summary>
		/// Complete if the player is alive and on one of the escape shuttles and shuttle has
		/// at least one working engine
		/// </summary>
		protected override bool CheckCompletion()
		{
			return ValidShuttles.Any( shuttle => shuttle.MatrixInfo != null && CheckForPlayersOnMatrix(shuttle.MatrixInfo.Matrix)) == false &&
			       CheckEscapePods();
		}

		private bool CheckEscapePods()
		{
			foreach (var pod in EscapePodGuidanceSystem.EscapePods)
			{
				var val = CheckForPlayersOnMatrix(pod.GetComponent<RegisterTile>().Matrix);
				if (val)
				{
					return true;
				}
			}

			return false;
		}


		private bool CheckForPlayersOnMatrix(Matrix Matrix)
		{
			foreach (var Player in Matrix.PresentPlayers)
			{
				if (Player.PlayerScript.playerHealth.IsDead) continue;

				if (AllowAntagonists)
				{
					if (Player.PlayerScript?.Mind?.IsAntag == true) continue;
				}

				if (AllowPlayerThemselves)
				{
					if (Player.PlayerScript?.Mind == Owner) continue;
				}

				return true;

			}
			return false;
		}
	}
}