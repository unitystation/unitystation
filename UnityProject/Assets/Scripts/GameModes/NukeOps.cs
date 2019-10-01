using UnityEngine;

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
	/// <summary>
	/// Check if the round should end yet
	/// </summary>
	public override void CheckEndCondition()
	{
		Logger.Log("Check end round conditions!", Category.GameMode);
	}
	/// <summary>
	/// End the round and display any relevant reports
	/// </summary>
	public override void EndRound()
	{
		Logger.Log("Ending round!", Category.GameMode);
	}

	// TODO
	// private void ChooseNukeOps()
	// {

	// }
}