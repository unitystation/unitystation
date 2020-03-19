using System;
using System.Collections.Generic;
using UnityEngine;
using Antagonists;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/NukeOps")]
public class NukeOps : GameMode
{
	[Tooltip("Ratio of nuke ops to player count. A value of 0.2 means there would be " +
	         "2 nuke ops when there are 10 players.")]
	[Range(0,1)]
	[SerializeField]
	private float nukeOpsRatio;


	/// <summary>
	/// Set up the station for the game mode
	/// </summary>
	public override void SetupRound()
	{
		Logger.Log("Setting up NukeOps round!", Category.GameMode);
	}
	/// <summary>
	/// Begin the round
	/// </summary>
	public override void StartRound()
	{
		Logger.Log("Starting NukeOps round!", Category.GameMode);
		base.StartRound();
	}

	public override bool IsPossible()
	{
		return base.IsPossible() && (FindObjectOfType<Nuke>() != null);
	}

	protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
	{
		//spawn only if there is not yet any syndicate ops (and at least one other player) or
		//the ratio is too low
		var existingNukeOps = PlayerList.Instance.AntagPlayers.Count;
		var inGamePlayers = PlayerList.Instance.InGamePlayers.Count;

		if ((inGamePlayers > 0 && existingNukeOps == 0) ||
			existingNukeOps < Math.Floor(inGamePlayers * nukeOpsRatio)) return true;

		return false;
	}
}
