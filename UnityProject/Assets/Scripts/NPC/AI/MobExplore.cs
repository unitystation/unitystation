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

	public bool performingDecision;
	public bool exploring;
	public float tickRate = 1f;
	private float tickWait = 0f;

	protected override void AgentServerStart()
	{
		exploring = true;
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

		//Observe adjacent tiles
		for (int y = 1; y > -2; y--)
		{
			for (int x = -1; x < 2; x++)
			{
				if (x == 0 && y == 0) continue;

				var checkPos = curPos;
				checkPos.x += x;
				checkPos.y += y;
				var passable = registerObj.Matrix.IsPassableAt(checkPos, true);

				//Record the passable observation:
				if (!passable)
				{
					//Is the path blocked because of a door?
					//Feed observations to AI
					DoorController tryGetDoor = registerObj.Matrix.GetFirst<DoorController>(checkPos, true);
					if (tryGetDoor != null)
					{
						//NPCs can open doors with no access restrictions
						if ((int) tryGetDoor.AccessRestrictions.restriction == 0)
						{
							AddVectorObs(true);
							// No the target is not here
							AddVectorObs(false);
						}
						else
						{
							//NPC does not have the access required
							//TODO: Allow id cards to be placed on mobs
							AddVectorObs(false);
							// No the target is not here
							AddVectorObs(false);
						}
					}
					else
					{
						AddVectorObs(false);
						// No the target is not here
						AddVectorObs(false);
					}
				}
				else
				{
					AddVectorObs(true);
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

				edible.NPCTryEat();
				break;
		}
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
	}

	void PerformMoveAction(int act)
	{
		//0 = no move (1 - 8 are directions to move in reading from left to right)
		if (act == 0)
		{
			performingDecision = false;
		}
		else
		{
			Vector2Int dirToMove = Vector2Int.zero;
			int count = 1;
			for (int y = 1; y > -2; y--)
			{
				for (int x = -1; x < 2; x++)
				{
					if (count == act)
					{
						dirToMove.x += x;
						dirToMove.y += y;
						y = -100;
						break;
					}
					else
					{
						count++;
					}
				}
			}

			var dest = registerObj.LocalPositionServer + (Vector3Int) dirToMove;

			if (!cnt.Push(dirToMove))
			{
				//Path is blocked try again
				performingDecision = false;
				DoorController tryGetDoor =
					registerObj.Matrix.GetFirst<DoorController>(
						dest, true);
				if (tryGetDoor)
				{
					tryGetDoor.TryOpen(gameObject);
				}
			}
			else
			{
				if (IsTargetFound(dest))
				{
					SetReward(1f);
					PerformTargetAction(dest);
				}
			}
		}
	}

	protected override void OnTileReached(Vector3Int tilePos)
	{
		if (performingDecision) performingDecision = false;
	}

	protected override void UpdateMe()
	{
		MonitorDecisionMaking();
	}

	void MonitorDecisionMaking()
	{
		tickWait += Time.deltaTime;
		if (tickWait >= tickRate)
		{
			tickWait = 0f;
			
			if (!exploring || performingDecision) return;
			performingDecision = true;
			RequestDecision();
		}
	}
}