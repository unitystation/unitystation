using UnityEngine;
using UnityEngine.Serialization;
using MLAgents;
using Doors;
using Objects;

namespace Systems.MobAIs
{
	/// <summary>
	/// Handles the underlying logic for
	/// the Mob[Brain] behaviours
	/// </summary>
	[RequireComponent(typeof(CustomNetTransform))]
	[RequireComponent(typeof(RegisterObject))]
	public class MobAgent : Agent, IServerLifecycle
	{
		protected CustomNetTransform cnt;
		protected RegisterObject registerObj;
		protected ObjectBehaviour objectBehaviour;
		protected Directional directional;
		protected LivingHealthBehaviour health; // For living beings
		protected Integrity integrity; // For bots

		private Vector3 startPos;

		protected bool isServer;

		public bool performingDecision;
		public bool performingAction;

		public bool activated;
		[Range(0.01f, 1), FormerlySerializedAs("tickRate")]
		[Tooltip("Delay (in seconds) between mob actions/decisions.")]
		public float TickDelay = 1f;
		private float tickWait;
		private float decisionTimeOut;

		public bool Pause { get; set; }
		protected RegisterTile OriginTile;

		private void Awake()
		{
			cnt = GetComponent<CustomNetTransform>();
			registerObj = GetComponent<RegisterObject>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			directional = GetComponent<Directional>();
			health = GetComponent<LivingHealthBehaviour>();
			integrity = GetComponent<Integrity>();
			OriginTile = GetComponent<RegisterTile>();
			agentParameters.onDemandDecision = true;
		}


		//Reset is used mainly for training
		//SetPosition() has now been commented out
		//as it was used in training. Leaving the
		//lines present for any future retraining
		public override void AgentReset()
		{
			//	cnt.SetPosition(startPos);
		}

		[ContextMenu("Force Activate")]
		public virtual void Activate()
		{
			activated = true;
		}

		public virtual void Deactivate()
		{
			activated = false;
			performingDecision = false;
			decisionTimeOut = 0f;
			tickWait = 0f;
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			UpdateManager.Add(CallbackType.UPDATE, ServerUpdateMe);
			cnt.OnTileReached().AddListener(OnTileReached);
			startPos = OriginTile.WorldPositionServer;
			isServer = true;
			AgentServerStart();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			cnt.OnTileReached().RemoveListener(OnTileReached);
			UpdateManager.Remove(CallbackType.UPDATE, ServerUpdateMe);
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

		/// <summary>
		/// Called when the mob is performing an action
		/// </summary>
		protected virtual void OnPerformAction()
		{
		}

		/// <summary>
		/// Make sure to call base.UpdateMe if overriding
		/// </summary>
		protected virtual void ServerUpdateMe()
		{
			if (MatrixManager.IsInitialized)
			{
				MonitorDecisionMaking();
			}
		}

		/// <summary>
		/// Convenience method for when the bot has been initialized
		/// successfully on the server side
		/// </summary>
		protected virtual void AgentServerStart()
		{
		}

		private void MonitorDecisionMaking()
		{
			// Only living mobs have health.  Some like the bots have integrity instead.
			if (health != null) // Living mob
			{
				if (health.IsDead || health.IsCrit)
				{
					// Can't do anything this NPC is not capable of movement
					return;
				}
			}
			else if (integrity != null) //Bot
			{
				if (integrity.integrity <= 0)
				{
					// Too damaged to move
					return;
				}
			}
			else
			{
				// Don't do anything without a health or integrity
				return;
			}

			if (Pause)
			{
				return;
			}

			// If the mob is already performing an action, it's not the time to make a decision yet.
			if (performingAction)
			{
				OnPerformAction();
				return;
			}

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

			if (tickWait >= TickDelay)
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
				return;
			}
			if (objectBehaviour.parentContainer != null)
			{
				foreach (var escapable in objectBehaviour.parentContainer.GetComponents<IEscapable>())
				{
					escapable.EntityTryEscape(gameObject);
				}
				return;
			}

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

			if (dirToMove == Vector2Int.zero)
			{
				performingDecision = false;
				return;
			}

			var dest = registerObj.LocalPositionServer + (Vector3Int)dirToMove;

			if (!cnt.Push(dirToMove, context: gameObject))
			{
				//Path is blocked try again
				performingDecision = false;
				DoorController tryGetDoor =
					registerObj.Matrix.GetFirst<DoorController>(
						dest, true);
				if (tryGetDoor)
				{
					tryGetDoor.MobTryOpen(gameObject);
				}

				//New doors
				DoorMasterController tryGetDoorMaster =
					registerObj.Matrix.GetFirst<DoorMasterController>(
						dest, true);
				if (tryGetDoorMaster)
				{
					tryGetDoorMaster.Bump(gameObject);
				}
			}
			else
			{
				OnPushSolid(dest);
			}

			if (directional != null)
			{
				directional.FaceDirection(Orientation.From(dirToMove));
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
		protected void ObserveAdjacentTiles(bool allowTargetPush = false, RegisterTile target = null)
		{
			var curPos = registerObj.LocalPositionServer;

			if (registerObj == null)
			{
				Logger.LogError($"RegisterObject is null for: {gameObject.name}. Pausing this MobAI", Category.Mobs);
				Pause = true;
				return;
			}
			//Observe adjacent tiles
			for (int y = 1; y > -2; y--)
			{
				for (int x = -1; x < 2; x++)
				{
					if (x == 0 && y == 0) continue;

					var checkPos = curPos;
					checkPos.x += x;
					checkPos.y += y;
					var passable = registerObj.Matrix.IsPassableAtOneMatrixOneTile(checkPos, true);

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
								if (target != null)
								{
									if (checkPos == target.LocalPositionServer)
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
							if (tryGetDoor.AccessRestrictions != null)
							{
								//NPCs can open doors with no access restrictions
								if ((int)tryGetDoor.AccessRestrictions.restriction == 0)
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
							else
							{
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
}
