using UnityEngine;
using UnityEngine.SceneManagement;

public class Extended : GameMode
{
	/// <summary>
	/// Set up the station for the game mode
	/// </summary>
	public override void SetupRound() {}
	/// <summary>
	/// Begin the round
	/// </summary>
	public override void StartRound()
	{
		Logger.Log("Starting extended round!", Category.GameMode);
		base.StartRound();
	}
	/// <summary>
	/// Check if the round should end yet
	/// </summary>
	public override void CheckEndCondition() {}
	/// <summary>
	/// End the round and display any relevant reports
	/// </summary>
	public override void EndRound() {}
}