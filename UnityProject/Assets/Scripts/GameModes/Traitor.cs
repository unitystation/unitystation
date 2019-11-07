using UnityEngine;
using Antagonists;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Traitor")]
public class Traitor : GameMode
{
	/// <summary>
	/// Set up the station for the game mode
	/// </summary>
	public override void SetupRound()
	{
		Logger.Log("Setting up traitor round!", Category.GameMode);
	}
	/// <summary>
	/// Begin the round
	/// </summary>
	public override void StartRound()
	{
		Logger.Log("Starting traitor round!", Category.GameMode);
		base.StartRound();
	}
	// /// <summary>
	// /// Check if the round should end yet
	// /// </summary>
	// public override void CheckEndCondition()
	// {
	// 	Logger.Log("Check end round conditions!", Category.GameMode);
	// }

	// /// <summary>
	// /// End the round and display any relevant reports
	// /// </summary>
	// public override void EndRound()
	// {

	// }

	/// <summary>
	/// Check if more antags are needed. Should be defined by each game mode.
	/// </summary>
	public override void CheckAntags()
	{
		if ((AntagManager.Instance.AntagCount == 0) && PlayerList.Instance.InGamePlayers.Count > 1)
		{
			SpawnAntag();
		}
	}
}