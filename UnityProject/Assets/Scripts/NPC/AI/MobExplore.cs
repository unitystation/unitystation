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

	protected override void AgentServerStart()
	{
		activated = true;
	}

	private List<Vector3Int> alreadyEaten = new List<Vector3Int>();

	public override void AgentReset()
	{
		base.AgentReset();
		alreadyEaten.Clear();
	}

	public override void CollectObservations()
	{
		var curPos = registerObj.LocalPositionServer;
		AddVectorObs((Vector2Int)curPos);

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
				if (registerObj.Matrix.GetFirst<Edible>(checkPos, true) != null &&
				    !alreadyEaten.Contains(checkPos)) return true;
				return false;
		}

		return false;
	}

	//This is really for training:
	void PerformTargetAction(Vector3Int checkPos)
	{
		switch (target)
		{
			case Target.food:
				var edible = registerObj.Matrix.GetFirst<Edible>(checkPos, true);
				if (edible != null)
				{
					alreadyEaten.Add(checkPos);
				}

			//	edible.NPCTryEat();
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