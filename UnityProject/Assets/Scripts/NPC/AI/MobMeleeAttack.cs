using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic AI behaviour for following and melee attacking a target
/// </summary>
[RequireComponent(typeof(MobAI))]
public class MobMeleeAttack : MobFollow
{
	protected override void OnPushSolid(Vector3Int destination)
	{
		ValidateAttack(destination);
	}

	//Check for anything meleeable on that position
	//also check tilemap for meleeable tiles
	private void ValidateAttack(Vector3Int destination)
	{
		var dir = destination - registerObj.LocalPositionServer;
		var worldPos = registerObj.WorldPositionServer + dir;

		var meleeable = MatrixManager.GetAt<Meleeable>(worldPos, true);
		if (meleeable.Count > 0)
		{
			//Attack anything meleeable in the way. Is it our target?
			return;
		}

		var interactableTiles = MatrixManager.GetAt<InteractableTiles>(worldPos, true);
		if (interactableTiles.Count > 0)
		{
			//attack tiles
			return;
		}
	}
}
