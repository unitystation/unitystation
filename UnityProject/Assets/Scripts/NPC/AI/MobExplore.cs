using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI brain specifically trained to explore
/// the surrounding area for specific objects
/// </summary>
public class MobExplore : MobAgent
{
	//Add your targets as needed
	public enum Target
	{
		food
	}

	public Target target;

	/// <summary>
	/// Begin searching for the predefined target
	/// </summary>
	public void BeginExploring()
	{
		Activate();
	}

	/// <summary>
	/// Begin exploring for the given target type
	/// </summary>
	/// <param name="target"></param>
	public void BeginExploring(Target _target)
	{
		target = _target;
		Activate();
	}

	public override void CollectObservations()
	{
		var curPos = registerObj.LocalPositionServer;

		ObserveAdjacentTiles();

		//Search surrounding tiles for the food
		for (int y = 1; y > -2; y--)
		{
			for (int x = -1; x < 2; x++)
			{
				if (x == 0 && y == 0) continue;

				var checkPos = curPos;
				checkPos.x += x;
				checkPos.y += y;

				if (IsTargetFound(checkPos))
				{
					// Yes the target is here!
					AddVectorObs(true);
				}
				else
				{
					AddVectorObs(false);
				}
			}
		}
	}

	bool IsTargetFound(Vector3Int checkPos)
	{
		switch (target)
		{
			case Target.food:
				if (registerObj.Matrix.GetFirst<Edible>(checkPos, true) != null) return true;
				return false;
		}

		return false;
	}

	/// <summary>
	/// Override this for custom target actions
	/// </summary>
	protected virtual void PerformTargetAction(Vector3Int checkPos)
	{
		switch (target)
		{
			case Target.food:
				var edible = registerObj.Matrix.GetFirst<Edible>(checkPos, true);
				edible.NPCTryEat();
				break;
		}
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
	}


	protected override void OnPushSolid(Vector3Int destination)
	{
		if (IsTargetFound(destination))
		{
			SetReward(1f);
			PerformTargetAction(destination);
		}
	}
}