using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Extended")]
public class Extended : GameMode
{
	/// <summary>
	/// Set up the station for the game mode
	/// </summary>
	public override void SetupRound() {}

	protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
	{
		//no antags
		return false;
	}

	/// <summary>
	/// Begin the round
	/// </summary>
	public override void StartRound()
	{
		Logger.Log("Starting extended round!", Category.GameMode);
		base.StartRound();
	}
}