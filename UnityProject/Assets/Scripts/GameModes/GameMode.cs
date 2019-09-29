using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Contains the definition of a game mode. To create a new one you should
/// make a new class which inherits this one. Make a prefab with this script
/// attached so you can define the values in the editor and define your
/// game mode functions in the child class.
/// </summary>
public abstract class GameMode : MonoBehaviour
{
	/// <summary>
	/// The name of the game mode
	/// </summary>
	[Tooltip("The name of the game mode")]
	public string Name;

	/// <summary>
	/// The description of the game mode
	/// </summary>
	[Tooltip("A description of the game mode")]
	public string Description;

	/// <summary>
	/// Is respawning enabled in this game mode
	/// </summary>
	[Tooltip("Should players be allowed to respawn?")]
	public bool CanRespawn;

	/// <summary>
	/// The minimum amount of players needed for the game mode
	/// </summary>
	[Tooltip("What is the minimum amount of players needed for this game mode?")]
	public int MinPlayers;

	/// <summary>
	/// The minimum amount of antags needed in the game mode
	/// </summary>
	[Tooltip("What is the minimum amount of antagonists needed for this game mode?")]
	public int MinAntags;

	// ================= Game Mode Methods =================

	/// <summary>
	/// Check if the game mode meets the minimum player requirements
	/// </summary>
	public virtual bool IsPossible()
	{
		//TODO add more checks in future
		return PlayerList.Instance.ConnectionCount >= MinPlayers;
	}

	/// <summary>
	/// Set up everything for the game mode
	/// </summary>
	public abstract void SetupRound();

	/// <summary>
	/// Determine what to do with new players that join
	/// </summary>
	// public virtual void SetupNewPlayer()
	// {
	// 	if (GameManager.Instance.RoundStarted)
	// 	{
	// 		UIManager.Display.SetScreenForJobSelect();
	// 	}
	// }

	/// <summary>
	/// Start the round
	/// </summary>
	public virtual void StartRound()
	{
		// TODO remove once random job allocation is done
		UpdateUIMessage.Send(ControlDisplays.Screens.JobSelect);
	}

	/// <summary>
	/// Check if the round should end yet
	/// </summary>
	public abstract void CheckEndCondition();

	/// <summary>
	/// End the round and display any relevant reports
	/// </summary>
	public abstract void EndRound();

	// TODO
	// public abstract void ChooseAntags();
}