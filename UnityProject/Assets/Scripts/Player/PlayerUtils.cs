using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utilities for working with players
/// </summary>
public static class PlayerUtils
{
	/// <summary>
	/// Check if the gameobject is a ghost
	/// </summary>
	/// <param name="playerObject">object controlled by a player</param>
	/// <returns>true iff playerObject is a ghost</returns>
	public static bool IsGhost(GameObject playerObject)
	{
		return playerObject.layer == 31;
	}
}
