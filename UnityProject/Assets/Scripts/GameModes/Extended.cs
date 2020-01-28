using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Extended")]
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
	public override void CheckEndCondition() {}
	public override void EndRound() {}
	public override void TrySpawnAntag() {}
}