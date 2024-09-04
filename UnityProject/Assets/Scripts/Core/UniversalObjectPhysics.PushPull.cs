using System;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using SecureStuff;
using UnityEngine;
using Util;

namespace Core
{
	public partial class UniversalObjectPhysics
	{
		//## NETWORKING ## //
		[SyncVar(hook = nameof(SyncIsNotPushable))]
		public bool isNotPushable;
		[SyncVar(hook = nameof(SynchroniseUpdatePulling))]
		private PullData ThisPullData;

		//## PULLING TARGETS ## //
		// TODO: Bod this is not what CheckedComponent is for as the reference is not on the same object as this script - Dan
		[PlayModeOnly] public CheckedComponent<UniversalObjectPhysics> Pulling = new();
		[PlayModeOnly] public CheckedComponent<UniversalObjectPhysics> PulledBy = new();
		public UniversalObjectPhysics DeepestPullingOrItself
		{
			get
			{
				if (Pulling.HasComponent)
				{
					return Pulling.Component.DeepestPullingOrItself;
				}
				else
				{
					return this;
				}
			}
		}

		//## PUSHING TARGETS ## //
		[HideInInspector] public List<UniversalObjectPhysics> Pushing = new List<UniversalObjectPhysics>();

		//## WIND AND OTHER ## //
		public bool CanBeWindPushed = true;

		public void PullSet(UniversalObjectPhysics toPull, bool byClient, bool synced = false)
		{
			if (toPull != null && ContainedInObjectContainer != null) return; //Can't pull stuff inside of objects)

			if (isServer && synced == false)
				SynchroniseUpdatePulling(ThisPullData,
					new PullData() {NewPulling = toPull, WasCausedByClient = byClient});

			if (toPull != null)
			{
				if (PulledBy.HasComponent)
				{
					if (toPull == PulledBy.Component)
					{
						PulledBy.Component.PullSet(null, false);
					}
				}

				if (Pulling.HasComponent)
				{
					Pulling.Component.PulledBy.SetToNull();
					Pulling.SetToNull();
					ContextGameObjects[1] = null;
				}

				if (toPull.IsNotPushable) return;

				Pulling.DirectSetComponent(toPull);
				toPull.PulledBy.DirectSetComponent(this);
				ContextGameObjects[1] = toPull.gameObject;
				if (isOwned) UIManager.Action.UpdatePullingUI(true);
			}
			else
			{
				if (isOwned) UIManager.Action.UpdatePullingUI(false);
				if (Pulling.HasComponent)
				{
					Pulling.Component.ResetClientPositionReachTile = true;
					Pulling.Component.SpecifiedClientPositionReachTile = netId;
					Pulling.Component.PulledBy.SetToNull();
					Pulling.SetToNull();
					ContextGameObjects[1] = null;
				}
			}
		}

		public void SynchroniseUpdatePulling(PullData oldPullData, PullData newPulling)
		{
			ThisPullData = newPulling;
			if (newPulling.WasCausedByClient && isOwned) return;
			PullSet(newPulling.NewPulling, false, true);
		}

		public void SetIsNotPushable(bool newState)
		{
			isNotPushable = newState;

			if (registerTile.Matrix != null) //Happens in initialisation/Start
			{
				//Force update atmos
				registerTile.Matrix.TileChangeManager.SubsystemManager.UpdateAt(
					OfficialPosition.ToLocalInt(registerTile.Matrix));
			}
		}

		private void SyncIsNotPushable(bool wasNotPushable, bool isNowNotPushable)
		{
			if (isNowNotPushable && PulledBy.HasComponent)
			{
				PulledBy.Component.StopPulling(false);
			}

			isNotPushable = isNowNotPushable;
		}

		public bool CanPush(Vector2Int worldDirection)
		{
			if (worldDirection == Vector2Int.zero) return true;
			if (CanMove == false) return false;
			if (PushedFrame == Time.frameCount)
			{
				var Direction = GetDecision(worldDirection);
				if (Direction != null)
				{
					return Direction.Value.Decision;
				}
			}

			if (TryPushedFrame == Time.frameCount)
			{
				return false;
			}

			TryPushedFrame = Time.frameCount;
			//TODO Secured stuff
			Pushing.Clear();
			Bumps.Clear();

			Vector3 from = transform.position;
			if (IsMoving) //We are moving combined targets
			{
				from = LocalTargetPosition.ToWorld(registerTile.Matrix);
			}

			SetMatrixCache.ResetNewPosition(from, registerTile);

			if (MatrixManager.IsPassableAtAllMatricesV2(from, from + worldDirection.To3Int(), SetMatrixCache, this, Pushing,
				    Bumps)) //Validate
			{
				if (PushedFrame != Time.frameCount)
				{
					TriedDirectionsFrame.Clear();
					PushedFrame = Time.frameCount;
				}

				TriedDirectionsFrame.Add(new DirectionAndDecision()
				{
					worldDirection = worldDirection,
					Decision = true
				});
				return true;
			}
			else
			{
				if (PushedFrame != Time.frameCount)
				{
					TriedDirectionsFrame.Clear();
					PushedFrame = Time.frameCount;
				}

				TriedDirectionsFrame.Add(new DirectionAndDecision()
				{
					worldDirection = worldDirection,
					Decision = false
				});
				return false;
			}
		}

		public void TryTilePush(Vector2Int worldDirection, GameObject byClient, float speed = Single.NaN,
			UniversalObjectPhysics pushedBy = null, bool overridePull = false, UniversalObjectPhysics pulledBy = null,
			bool useWorld = false)
		{
			if (isFlyingSliding) return;
			if (isVisible == false) return;
			if (pushedBy == this) return;
			if (CanPush(worldDirection))
			{
				if (isServer == false && byClient != PlayerManager.LocalPlayerObject)
				{
					Pushing.Clear();
				}

				ForceTilePush(worldDirection, Pushing, byClient, speed, pushedBy: pushedBy, overridePull: overridePull,
					pulledBy: pulledBy, SendWorld: useWorld);
			}
		}

		public void ForceTilePush(Vector2Int worldDirection, List<UniversalObjectPhysics> inPushing, GameObject byClient,
			float speed = Single.NaN, bool isWalk = false,
			UniversalObjectPhysics pushedBy = null, bool overridePull = false, UniversalObjectPhysics pulledBy = null,
			bool SendWorld = false)
		{
			if (isFlyingSliding) return;
			if (isVisible == false) return;
			if (ForcedPushedFrame == Time.frameCount) return;

			ForcedPushedFrame = Time.frameCount;
			if (CanMove == false) return;

			if (PulledBy.HasComponent && pulledBy == null)
			{
				PulledBy.Component.PullSet(null, false);
			}

			doNotApplyMomentumOnTarget = false;
			if (float.IsNaN(speed))
			{
				speed = CurrentTileMoveSpeed;
			}

			if (inPushing.Count > 0 && registerTile.IsPassable(isServer) == false)
			{
				foreach (var push in inPushing)
				{
					if (push == this || push == pushedBy || push.Intangible) continue;
					if (Pulling.HasComponent && Pulling.Component == push) continue;
					if (PulledBy.HasComponent && PulledBy.Component == push) continue;

					if (pushedBy == null)
					{
						pushedBy = this;
					}

					var pushDirection = worldDirection;
					push.TryTilePush(pushDirection, byClient, speed, pushedBy);
				}
			}

			var cachedPosition = transform.position;
			if (IsMoving)
			{
				cachedPosition = LocalTargetPosition.ToWorld(registerTile.Matrix);
			}

			var newWorldPosition = cachedPosition + worldDirection.To3Int();

			if (isServer && (newWorldPosition - transform.position).magnitude > 1.45f) return;

			var movetoMatrix = SetMatrixCache.GetforDirection(worldDirection.To3Int()).Matrix;

			if (registerTile.Matrix != movetoMatrix)
			{
				SetMatrix(movetoMatrix);
			}

			if (ChangesDirectionPush)
			{
				rotatable.OrNull()?.SetFaceDirectionLocalVector(worldDirection);
			}

			var localPosition = (newWorldPosition).ToLocal(movetoMatrix);

			SetLocalTarget = new Vector3WithData()
			{
				Vector3 = localPosition.RoundToInt(),
				ByClient = byClient.NetId(),
				Matrix = movetoMatrix.Id
			};

			MoveIsWalking = isWalk;

			if (Animating == false)
			{
				Animating = true;
				UpdateManager.Add(CallbackType.EARLY_UPDATE, AnimationUpdateMe);
				localTileMoveSpeedOverride = speed;
				if (isServer)
				{
					networkedTileMoveSpeedOverride = localTileMoveSpeedOverride;
				}
			}

			if (isServer && (PulledBy.HasComponent == false || overridePull))
			{
				SetTimestampID = Time.frameCount;
				if (SendWorld == false && connectionToClient != null && isServer)
				{
					int idbyClient = (int) NetId.Empty;
					if (byClient != null)
					{
						idbyClient = (int) byClient.NetId();
					}

					RPCClientTilePush(worldDirection, speed, idbyClient, overridePull, SetTimestampID, false);
				}
			}

			if (Pulling.HasComponent)
			{
				var inDirection = cachedPosition - Pulling.Component.transform.position;
				if (inDirection.magnitude > 2f && isServer)
				{
					PullSet(null, false);
				}
				else
				{
					Pulling.Component.SetMatrixCache.ResetNewPosition(Pulling.Component.transform.position);
					Pulling.Component.Pushing.Clear();
					Pulling.Component.ForceTilePush(inDirection.NormalizeTo2Int(), Pulling.Component.Pushing, byClient,
						speed, pulledBy: this);
				}
			}

			if (ObjectIsBuckling != null && ObjectIsBuckling.Pulling.HasComponent)
			{
				var inDirection = cachedPosition;
				if (inDirection.magnitude > 2f && (isServer || isOwned))
				{
					ObjectIsBuckling.PullSet(null, false); //TODO maybe remove
					if (ObjectIsBuckling.isOwned && isServer == false)
					{
						ObjectIsBuckling.CmdStopPulling();
					}
				}
				else
				{
					ObjectIsBuckling.Pulling.Component.SetMatrixCache.ResetNewPosition(
						ObjectIsBuckling.Pulling.Component.transform.position);
					ObjectIsBuckling.Pulling.Component.Pushing.Clear();
					ObjectIsBuckling.Pulling.Component.ForceTilePush(inDirection.NormalizeTo2Int(),
						ObjectIsBuckling.Pulling.Component.Pushing, byClient, speed, pulledBy: this);
				}
			}
		}

		public void ClientTryTogglePull()
		{
			var initiator = PlayerManager.LocalPlayerScript.GetComponent<UniversalObjectPhysics>();
			float interactDist = PlayerScript.INTERACTION_DISTANCE;
			var reachRange = ReachRange.Standard;
			if (PlayerManager.LocalPlayerScript.playerHealth.brain != null &&
			    PlayerManager.LocalPlayerScript.playerHealth.brain.HasTelekinesis) //Has telekinesis
			{
				interactDist = Validations.TELEKINESIS_INTERACTION_DISTANCE;
				reachRange = ReachRange.Telekinesis;
			}


			if (Validations.CanApply(PlayerManager.LocalPlayerScript, gameObject, NetworkSide.Client
				    , apt: Validations.CheckState(x => x.CanPull), reachRange: reachRange) == false)
			{
				return;
			}

			//client pre-validation
			if (Validations.IsReachableByRegisterTiles(initiator.registerTile, this.registerTile, false,
				    context: gameObject, interactDist: interactDist) && initiator != this)
			{
				if ((initiator.gameObject.AssumedWorldPosServer() - this.gameObject.AssumedWorldPosServer()).magnitude >
				    PlayerScript.INTERACTION_DISTANCE_EXTENDED) //If telekinesis was used play effect
				{
					PlayEffect.SendToAll(this.gameObject, "TelekinesisEffect");
				}

				//client request: start/stop pulling
				if (PulledBy.Component == initiator)
				{
					initiator.PullSet(null, true);
					initiator.CmdStopPulling();
				}
				else
				{
					if (this.isNotPushable) return;
					initiator.PullSet(this, true);
					initiator.CmdPullObject(gameObject);
				}
			}
			else
			{
				initiator.PullSet(null, true);
				initiator.CmdStopPulling();
			}
		}

		[Command]
		public void CmdPullObject(GameObject pullableObject)
		{
			if (ContainedInObjectContainer != null) return; //Can't pull stuff inside of objects
			if (pullableObject == null || pullableObject == this.gameObject) return;
			var pullable = pullableObject.GetComponent<UniversalObjectPhysics>();
			if (pullable == null || pullable.isNotPushable)
			{
				return;
			}

			if (Pulling.HasComponent)
			{
				//Just stopping pulling of object if we try pulling it again
				if (Pulling.Component == pullable)
				{
					return;
				}

				PullSet(null, true);
			}

			PlayerInfo clientWhoAsked = PlayerList.Instance.Get(gameObject);
			var reachRange = ReachRange.Standard;
			if (clientWhoAsked.Script.playerHealth.brain != null &&
			    clientWhoAsked.Script.playerHealth.brain.HasTelekinesis) //Has telekinesis
			{
				reachRange = ReachRange.Telekinesis;
			}

			if (Validations.CanApply(clientWhoAsked.Script, gameObject, NetworkSide.Server
				    , apt: Validations.CheckState(x => x.CanPull), reachRange: reachRange) == false)
			{
				return;
			}

			PullSet(pullable, true);
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.ThudSwoosh, pullable.transform.position,
				sourceObj: pullableObject);

			//TODO Update the UI
		}

		/// Client requests to stop pulling any objects
		[Command]
		public void CmdStopPulling()
		{
			PullSet(null, true);
		}

		public void StopPulling(bool byClient)
		{
			if (isServer == false) CmdStopPulling();
			PullSet(null, byClient);
		}

		public struct PullData : IEquatable<PullData>
		{
			public UniversalObjectPhysics NewPulling;
			public bool WasCausedByClient;

			public override bool Equals(object obj)
			{
				return obj is PullData other && Equals(other);
			}

			public bool Equals(PullData other)
			{
				return Equals(NewPulling, other.NewPulling) && WasCausedByClient == other.WasCausedByClient;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(NewPulling, WasCausedByClient);
			}
		}

	}
}