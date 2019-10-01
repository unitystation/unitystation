using UnityEngine;

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
	// private void ChooseTraitors()
	// {

	// }
}