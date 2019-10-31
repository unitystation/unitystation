using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName="ScriptableObjects/GameModeData")]
public class GameModeData : ScriptableObject
{
	/// <summary>
	/// All possible gamemodes
	/// </summary>
	[SerializeField]
	private List<GameMode> GameModes = new List<GameMode>();

	/// <summary>
	/// The default gamemode to pick
	/// </summary>
	[SerializeField]
	private GameMode DefaultGameMode;

	public GameMode GetGameMode(string gmName)
	{
		foreach(GameMode gm in GameModes)
		{
			if (gm.Name == gmName)
			{

				return gm;
			}
		}
		Logger.Log($"Unable to get gamemode {gmName}, returning default: {DefaultGameMode.Name}", Category.GameMode);
		return DefaultGameMode;
	}

	/// <summary>
	/// Randomly chooses a gamemode that is possible with the current number of players
	/// </summary>
	public GameMode ChooseGameMode()
	{
		List<GameMode> possibleGMs = GameModes.Where( gm => gm.IsPossible()).ToList();
		if (possibleGMs.Count == 0)
		{
			return DefaultGameMode;
		}

		return possibleGMs.PickRandom();
	}

}
