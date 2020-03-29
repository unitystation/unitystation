using UnityEngine;
using Antagonists;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Traitor")]
public class Traitor : GameMode
{

	[Tooltip("Ratio of traitors to player count. A value of 0.2 means there would be " +
			 "2 traitors when there are 10 players.")]
	[Range(0, 1)]
	[SerializeField]
	private float TraitorRatio;

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

	private List<JobType> LoyalImplanted = new List<JobType> 
	{
		JobType.CAPTAIN,
		JobType.HOP,
		JobType.HOS,
		JobType.WARDEN,
		JobType.SECURITY_OFFICER,
		JobType.DETECTIVE,
	};

	protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
	{
			// Populates traitors based on the ratio set
			return !LoyalImplanted.Contains(spawnRequest.RequestedOccupation.JobType)
					&& AntagManager.Instance.AntagCount <= Math.Floor(PlayerList.Instance.InGamePlayers.Count * TraitorRatio)
					&& PlayerList.Instance.InGamePlayers.Count > 0;

	}
}
