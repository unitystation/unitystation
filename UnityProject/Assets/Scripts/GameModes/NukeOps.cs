using System.Collections.Generic;
using UnityEngine;
using Antagonists;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/NukeOps")]
public class NukeOps : GameMode
{
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
		// TODO remove once random antag allocation is done
		UpdateUIMessage.Send(ControlDisplays.Screens.TeamSelect);
	}

	public override void CheckAntags()
	{
		List<ConnectedPlayer> shouldBeAntags = PlayerList.Instance.NonAntagPlayers.FindAll( p => p.Script.mind.occupation.JobType == JobType.SYNDICATE);
		foreach (var player in shouldBeAntags)
		{
			SpawnAntag(player);
		}
	}
}