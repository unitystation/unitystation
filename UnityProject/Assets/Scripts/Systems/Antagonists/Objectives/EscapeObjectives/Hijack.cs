using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Antagonists
{
	/// <summary>
	/// Hijack the Emergency Shuttle by Escaping Alone
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/Hijack")]
	public class Hijack : Objective
	{
		/// <summary>
		/// The shuttles that will be checked for this objective
		/// </summary>
		private List<EscapeShuttle> ValidShuttles = new List<EscapeShuttle>();
		
		/// <summary>
		/// Number of players needed in game for the objective to be possible
		/// </summary>
		[SerializeField]
		private int numberOfPlayersRequired = 20;
		
		/// <summary>
		/// If the objective allowed to be given to the antag
		/// </summary>
		protected override bool IsPossibleInternal(PlayerScript candidate)
		{
			if ((GameManager.Instance.CurrentRoundState == RoundState.PreRound ?
				    PlayerList.Instance.ReadyPlayers.Count : PlayerList.Instance.InGamePlayers.Count)
			    >= numberOfPlayersRequired)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Populate the list of valid escape shuttles
		/// </summary>
		protected override void Setup()
		{
			ValidShuttles.Add(GameManager.Instance.PrimaryEscapeShuttle);
		}

		/// <summary>
		/// Complete if the player is alive and on one of the escape shuttles and shuttle has
		/// at least one working engine, and is the only alive player on the shuttle
		/// </summary>
		protected override bool CheckCompletion()
		{
			//Must be alive
			if (Owner.body.IsDeadOrGhost)
			{
				return false;
			}

			//Shuttle must be functional and player be on it
			if (!ValidShuttles.Any( shuttle => shuttle.MatrixInfo != null
				&& Owner.body.registerTile.Matrix.Id == shuttle.MatrixInfo.Id && shuttle.HasWorkingThrusters))
			{
				return false;
			}

			//Be the only alive player on shuttle
			foreach (var player in PlayerList.Instance.InGamePlayers)
			{
				//Dont check dead, ghosts or self
				if(player.Script.IsDeadOrGhost || player.Script == Owner.body) continue;

				//TODO add check to ignore alive ghost critters, eg drones

				//The other players must not be on same shuttle to pass checks
				if (player.Script.registerTile.Matrix.Id == Owner.body.registerTile.Matrix.Id)
				{
					return false;
				}
			}

			return true;
		}
	}
}
