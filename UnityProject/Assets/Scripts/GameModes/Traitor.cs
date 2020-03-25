using UnityEngine;
using Antagonists;
using System.Collections.Generic;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Traitor")]
public class Traitor : GameMode
{
	
	private float TraitorAmount = 0;

	/// <summary>
	/// Set up the station for the game mode
	/// </summary>
	public override void SetupRound()
	{
		Logger.Log("Setting up traitor round!", Category.GameMode);

		//Populates traitors based on server population
		if(PlayerList.Instance.InGamePlayers.Count <= 150 && PlayerList.Instance.InGamePlayers.Count >= 50)
		{
			TraitorAmount = 6;
		}
			else if(PlayerList.Instance.InGamePlayers.Count < 50 && PlayerList.Instance.InGamePlayers.Count >= 10)
		{
				TraitorAmount = 4;
		}
			else
		{
				TraitorAmount = 1;
		}
		

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

		for (int i = 0; i < TraitorAmount - 1; i++) {
			return !LoyalImplanted.Contains(spawnRequest.RequestedOccupation.JobType)
					&& AntagManager.Instance.AntagCount == 0
					&& PlayerList.Instance.InGamePlayers.Count > 0;
		}
		return !LoyalImplanted.Contains(spawnRequest.RequestedOccupation.JobType)
					&& AntagManager.Instance.AntagCount == 0
					&& PlayerList.Instance.InGamePlayers.Count > 0;
	}
}