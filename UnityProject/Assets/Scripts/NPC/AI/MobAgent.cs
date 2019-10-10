using UnityEngine;
using MLAgents;

/// <summary>
/// Handles the underlying logic for
/// the Mob[Brain] behaviours
/// </summary>
[RequireComponent(typeof(CustomNetTransform))]
[RequireComponent(typeof(RegisterObject))]
public class MobAgent : Agent
{
	protected CustomNetTransform cnt;
	protected RegisterObject registerObj;

	private Vector3 startPos;

	protected bool isServer;

	public bool performingDecision;
	public bool activated;
	public float tickRate = 1f;
	private float tickWait;
	private float decisionTimeOut;

	void Awake()
	{
		cnt = GetComponent<CustomNetTransform>();
		registerObj = GetComponent<RegisterObject>();
		agentParameters.onDemandDecision = true;
	}

	//Reset is used mainly for training
	//SetPosition() has now been commented out
	//as it was used in training. Leaving the
	//lines present for any future retraining
	public override void AgentReset()
	{
		cnt.SetPosition(startPos);
	}

	[ContextMenu("Force Activate")]
	void ForceActivate()
	{
		activated = true;
	}

	public override void OnEnable()
	{
		//only needed for starting via a map scene through the editor:
		if (CustomNetworkManager.Instance == null) return;

		if (CustomNetworkManager.Instance._isServer)
		{
			cnt.OnTileReached().AddListener(OnTileReached);
			UpdateManager.Instance.Add(UpdateMe);
			startPos = transform.position;
			isServer = true;
			base.OnEnable();
			AgentServerStart();
		}
	}

	public override void OnDisable()
	{
		base.OnDisable();
		if (isServer)
		{
			cnt.OnTileReached().RemoveListener(OnTileReached);
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	protected virtual void OnTileReached(Vector3Int tilePos)
	{
		if (performingDecision) performingDecision = false;
	}

	/// <summary>
	/// Called when the mob has tried to move into a non passable area
	/// </summary>
	/// <param name="destination">The local matrix co-ords of the unpassable tile</param>
	protected virtual void OnPushSolid(Vector3Int destination)
	{
	}

	protected virtual void UpdateMe()
	{
		MonitorDecisionMaking();
	}

	/// <summary>
	/// Convenience method for when the bot has been initialized
	/// successfully on the server side
	/// </summary>
	protected virtual void AgentServerStart()
	{
	}

	void MonitorDecisionMaking()
	{
		tickWait += Time.deltaTime;

		if (performingDecision && activated)
		{
			decisionTimeOut += Time.deltaTime;
			if (decisionTimeOut > 10f)
			{
				decisionTimeOut = 0f;
				//The NPC could be stuck, lets
				//try another move
				RequestDecision();
			}
		}

		if (tickWait >= tickRate)
		{
			tickWait = 0f;

			if (!activated || performingDecision) return;
			decisionTimeOut = 0f;
			performingDecision = true;
			RequestDecision();
		}
	}

	/// <summary>
	/// General use movement for npcs
	/// 0 = no move (1 - 8 are directions to move in reading from left to right)
	/// </summary>
	/// <param name="act"></param>
	protected void PerformMoveAction(int act)
	{
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
				OnPushSolid(dest);
			}
		}
	}

	/// <summary>
	/// General use observation about the passable state
	/// of each surrounding tile. Set allowTargetPush to true
	/// to give your NPC the option of walking into the solid tile if it
	/// is your target.
	///
	/// This observation method uses 8 observation vectors. So remember to
	/// add them to your brain
	/// </summary>
	protected void ObserveAdjacentTiles(bool allowTargetPush = false, Transform target = null)
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
					if (tryGetDoor == null)
					{
						if (allowTargetPush)
						{
							if (checkPos == Vector3Int.RoundToInt(target.localPosition))
							{
								//it is our target! Allow mob to attempt to walk into it (can be used for attacking)
								AddVectorObs(true);
							}
							else
							{
								// it is something solid
								AddVectorObs(false);
							}
						}
						else
						{
							// it is something solid
							AddVectorObs(false);
						}
					}
					else
					{
						//NPCs can open doors with no access restrictions
						if ((int) tryGetDoor.AccessRestrictions.restriction == 0)
						{
							AddVectorObs(true);
						}
						else
						{
							//NPC does not have the access required
							//TODO: Allow id cards to be placed on mobs
							AddVectorObs(false);
						}
					}
				}
				else
				{
					AddVectorObs(true);
				}
			}
		}
	}
}