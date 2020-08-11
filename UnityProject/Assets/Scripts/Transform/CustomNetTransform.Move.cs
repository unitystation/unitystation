using System;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public enum SpinMode
{
	None,
	Clockwise,
	CounterClockwise
}

public partial class CustomNetTransform
{

	private const string STOPPED_FLOATING = "{0} stopped floating";
	private const string PREDICTIVE_STOP_TO = "{0}: predictive stop @ {1} to {2}";
	private const string NUDGE = "Nudge:{0} {1}";
	private PushPull pushPull;
	public PushPull PushPull => pushPull ? pushPull : (pushPull = GetComponent<PushPull>());

	/// Containers and other objects meant to be snapped by tile
	public bool IsTileSnap => registerTile.ObjectType == ObjectType.Object;

	public bool IsClientLerping => transform.localPosition != MatrixManager.WorldToLocal(predictedState.WorldPosition, MatrixManager.Get(matrix));
	public bool IsServerLerping => serverLerpState.WorldPosition != serverState.WorldPosition;
	public bool CanPredictPush => !IsClientLerping;
	public bool IsMovingClient => IsClientLerping;
	public bool IsMovingServer => IsServerLerping;
	public Vector2 ServerImpulse => serverState.WorldImpulse;
	public float SpeedServer => ServerState.speed;
	public float SpeedClient => PredictedState.speed;
	public bool IsFloatingServer => serverState.WorldImpulse != Vector2.zero && serverState.Speed > 0f && !IsBeingPulledServer;
	public bool IsFloatingClient => predictedState.WorldImpulse != Vector2.zero && predictedState.Speed > 0f && !IsBeingPulledClient;
	public bool IsBeingThrownServer => !serverState.ActiveThrow.Equals(ThrowInfo.NoThrow);
	public bool IsBeingThrownClient => !clientState.ActiveThrow.Equals(ThrowInfo.NoThrow);
	public bool IsBeingPulledServer => pushPull && pushPull.IsBeingPulled;
	public bool IsBeingPulledClient => pushPull && pushPull.IsBeingPulledClient;

	/// (Server) Did the flying item reach the planned landing point?
	private bool ShouldStopThrow
	{
		get
		{
			if (!IsBeingThrownServer && !IsBeingThrownClient)
			{
				return true;
			}

			bool shouldStop =
				Vector3.Distance(serverState.ActiveThrow.OriginWorldPos, serverState.WorldPosition) >= serverState.ActiveThrow.WorldTrajectory.magnitude;
			//			if ( shouldStop ) {
			//				Logger.Log( $"Should stop throw: {Vector3.Distance( serverState.ActiveThrow.OriginPos, serverState.WorldPosition )}" +
			//				           $" >= {trajectory.magnitude}" );
			//			}
			return shouldStop;
		}
	}

	public void NewtonianMove(Vector2Int direction, float speed = Single.NaN)
	{
		PushInternal(direction, true, speed);
	}

	/// <summary>
	/// Push this thing in provided direction
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="speed"></param>
	/// <param name="followMode">flag used when object is following its puller
	/// (turns on tile snapping and removes player collision check)</param>
	/// <returns>true if push was successful</returns>
	[Server]
	public bool Push(Vector2Int direction, float speed = Single.NaN, bool followMode = false, bool ignorePassable = false)
	{
		return PushInternal(direction, isNewtonian: false, speed: speed, followMode: followMode, ignorePassable: ignorePassable);
	}

	private bool PushInternal(
			Vector2Int direction, bool isNewtonian = false, float speed = Single.NaN, bool followMode = false, bool ignorePassable = false)
	{
		if (!float.IsNaN(speed) && speed <= 0)
		{
			return false;
		}

		Vector3Int clampedDir = direction.NormalizeTo3Int();
		Vector3Int origin = ServerPosition;
		Vector3Int roundedTarget = origin + clampedDir;

		if (!ignorePassable && !MatrixManager.IsPassableAt(origin, roundedTarget, true, includingPlayers: !followMode))
		{
			return false;
		}

		if (isNewtonian && !MatrixManager.IsSlipperyOrNoGravityAt(roundedTarget))
		{
			return false;
		}

		if (!followMode && MatrixManager.IsEmptyAt(roundedTarget, true))
		{
			serverState.WorldImpulse = (Vector3)clampedDir;
		}
		else
		{
			serverState.WorldImpulse = Vector2.zero;
		}

		if (!float.IsNaN(speed) && speed > 0)
		{
			serverState.Speed = speed;
		}
		else
		{
			serverState.Speed = PushPull.DEFAULT_PUSH_SPEED;
		}

		if (followMode)
		{
			serverState.IsFollowUpdate = true;
			SetPosition(roundedTarget);
			serverState.IsFollowUpdate = false;
		}
		else
		{
			SetPosition(roundedTarget);
		}

		return true;
	}

	public bool PredictivePush(Vector2Int target, float speed = Single.NaN, bool followMode = false)
	{
		Poke();
		Vector3Int target3int = target.To3Int();

		Vector3Int currentPos = ClientPosition;

		if (!followMode && !MatrixManager.IsPassableAt(target3int, target3int, isServer : false))
		{
			return false;
		}

		if (!followMode && MatrixManager.IsEmptyAt(target3int, false))
		{
			predictedState.WorldImpulse = target - currentPos.To2Int();
		}
		else
		{
			predictedState.WorldImpulse = Vector2.zero;
		}

		if (!float.IsNaN(speed) && speed > 0)
		{
			predictedState.Speed = speed;
		}
		else
		{
			predictedState.Speed = PushPull.DEFAULT_PUSH_SPEED;
		}

		predictedState.MatrixId = MatrixManager.AtPoint(target3int, false).Id;
		predictedState.WorldPosition = target3int;

		//		Lerp to compensate one frame delay
		Lerp();

		return true;
	}

	/// <summary>
	/// Stop floating, kill impulse
	/// </summary>
	public void Stop()
	{
		Stop(true);
	}

	private void Stop( bool notify )
	{
		Logger.LogTraceFormat(STOPPED_FLOATING, Category.Transform, gameObject.name);
        if (IsTileSnap)
        {
        	serverState.Position = Vector3Int.RoundToInt(serverState.Position);
        }
        else
        {
        	serverState.Speed = 0;
        }
        serverState.WorldImpulse = Vector2.zero;
        serverState.SpinRotation = transform.localRotation.eulerAngles.z;
        serverState.SpinFactor = 0;

        if ( IsBeingThrownServer )
        {
			OnThrowEnd.Invoke(serverState.ActiveThrow);
        }
        serverState.ActiveThrow = ThrowInfo.NoThrow;
        if ( notify )
        {
			NotifyPlayers();
        }
        registerTile.UpdatePositionServer();
	}

	public void OnClientStartFollowing(){}

	public void OnClientStopFollowing(){}

	/// <summary>
	/// Predictive client movement
	/// Mimics server collision checks for obviously impassable things.
	/// That prevents objects going through walls if server doesn't respond in time
	/// </summary>
	/// <returns>true if transform has changed</returns>
	private bool CheckFloatingClient()
	{
		return CheckFloatingClient(TransformState.HiddenPos);
	}

	/// <summary>
	/// internal method, called recursively if more than one tile has passed within one frame
	/// </summary>
	private bool CheckFloatingClient(Vector3 goal)
	{
		bool isRecursive = goal != TransformState.HiddenPos;
		if (!IsFloatingClient)
		{
			return isRecursive;
		}
		Vector3 worldPos = predictedState.WorldPosition;
		Vector3Int intOrigin = Vector3Int.RoundToInt(worldPos);

		Vector3 moveDelta;
		if (!isRecursive)
		{ //Normal delta if not recursive
			moveDelta = (Vector3)predictedState.WorldImpulse * predictedState.Speed * Time.deltaTime;
		}
		else
		{ //Artificial delta if recursive
			moveDelta = goal - worldPos;
		}

		float distance = moveDelta.magnitude;
		Vector3 newGoal;

		if (distance > 1)
		{
			//limit goal to just one tile away and run this method recursively afterwards
			newGoal = worldPos + (Vector3)predictedState.WorldImpulse;
		}
		else
		{
			newGoal = worldPos + moveDelta;
		}
		Vector3Int intGoal = Vector3Int.RoundToInt(newGoal);

		bool isWithinTile = intOrigin == intGoal; //same tile, no need to validate stuff
		if (isWithinTile || CanDriftTo(intOrigin, intGoal, isServer : false))
		{
			//advance
			predictedState.WorldPosition += moveDelta;
		}
		else
		{
			//stop
			Logger.LogTraceFormat(PREDICTIVE_STOP_TO, Category.Transform, gameObject.name, worldPos, intGoal);
			//			clientState.Speed = 0f;
			predictedState.WorldImpulse = Vector2.zero;
			predictedState.SpinFactor = 0;
		}

		if (distance > 1)
		{
			CheckFloatingClient(isRecursive ? goal : newGoal);
		}

		return true;
	}

	/// Clientside lerping (transform to clientState position)
	private void Lerp()
	{
		var worldPos = predictedState.WorldPosition;
		Vector3 targetPos = worldPos.ToLocal(matrix);
		//Set position immediately if not moving
		if (predictedState.Speed.Equals(0))
		{
			transform.localPosition = targetPos;
			OnClientTileReached().Invoke(worldPos.RoundToInt());
			return;
		}
		transform.localPosition =
			Vector3.MoveTowards(transform.localPosition, targetPos,
				predictedState.Speed * Time.deltaTime * transform.localPosition.SpeedTo(targetPos));
		if (transform.localPosition == targetPos)
		{
			OnClientTileReached().Invoke(predictedState.WorldPosition.RoundToInt());
		}
	}

	/// Serverside lerping
	private void ServerLerp()
	{
		Vector3 worldPos = serverState.WorldPosition;
		Vector3 targetPos = worldPos.ToLocal(matrix);
		//Set position immediately if not moving
		if (serverState.Speed.Equals(0))
		{
			serverLerpState = serverState;
			ServerOnTileReached(worldPos.RoundToInt());
			return;
		}
		serverLerpState.Position =
			Vector3.MoveTowards(serverLerpState.Position, targetPos,
				serverState.Speed * Time.deltaTime * serverLerpState.Position.SpeedTo(targetPos));

		if (serverLerpState.Position == targetPos)
		{
			ServerOnTileReached(serverState.WorldPosition.RoundToInt());
		}
	}

	/// Drop with some inertia.
	/// Currently used by players dropping something while space floating
	[Server]
	public void InertiaDrop(Vector3 initialPos, float speed, Vector2 impulse)
	{
		SetPosition(initialPos, false);
		serverState.WorldImpulse = impulse;
		serverState.Speed = Mathf.Clamp(Random.Range(-1.5f, -0.1f) + speed, 0, float.MaxValue);
		NotifyPlayers();
	}

	/// Throw object using data provided in ThrowInfo.
	/// Range will be limited by itemAttributes
	[Server]
	public void Throw(ThrowInfo info)
	{
		OnThrowStart.Invoke(info);

		SetPosition(info.OriginWorldPos, false);

		float throwSpeed = ItemAttributes.ThrowSpeed * 10; //tiles per second
		float throwRange = ItemAttributes.ThrowRange;

		Vector2 worldImpulse = info.WorldTrajectory.normalized;

		var correctedInfo = info;
		//limit throw range here
		if (info.WorldTrajectory.magnitude > throwRange)
		{
			correctedInfo.WorldTrajectory = Vector3.ClampMagnitude(info.WorldTrajectory, throwRange);
			//			Logger.Log( $"Throw distance clamped to {correctedInfo.Trajectory.magnitude}, " +
			//			           $"target changed {info.TargetPos}->{correctedInfo.TargetPos}" );
		}

		//add player momentum
		float playerMomentum = 0f;
		//If throwing nearby, do so at 1/2 speed (looks clunky otherwise)
		float speedMultiplier = Mathf.Clamp(correctedInfo.WorldTrajectory.magnitude / (throwRange <= 0 ? 1 : throwRange), 0.6f, 1f);
		serverState.Speed = (Random.Range(-0.2f, 0.2f) + throwSpeed + playerMomentum) * speedMultiplier;
		correctedInfo.InitialSpeed = serverState.Speed;

		serverState.WorldImpulse = worldImpulse;
		if (info.SpinMode != SpinMode.None)
		{
			serverState.SpinFactor = (sbyte)(Mathf.Clamp(throwSpeed * (2f / (int)ItemAttributes.Size + 1), sbyte.MinValue, sbyte.MaxValue) *
				(info.SpinMode == SpinMode.Clockwise ? 1 : -1));
		}
		serverState.ActiveThrow = correctedInfo;
		//		Logger.Log( $"Throw:{correctedInfo} {serverState}" );
		NotifyPlayers();
	}

	/// <summary>
	/// Experimental.
	/// Nudge object (sliding on the ground, not in the air)
	/// </summary>
	[Server]
	public void Nudge(NudgeInfo info ) {

		if ( PushPull.IsNotPushable )
        {
            return;
        }

		Vector2 impulse = info.Trajectory.normalized;

		serverState.Speed = info.InitialSpeed;

		serverState.WorldImpulse = impulse;
		if (info.SpinMode != SpinMode.None)
		{
			if (info.SpinMultiplier <= 0)
			{
				info.SpinMultiplier = 1;
			}
			serverState.SpinFactor = (sbyte)(Mathf.Clamp(info.InitialSpeed * info.SpinMultiplier, sbyte.MinValue, sbyte.MaxValue) *
				(info.SpinMode == SpinMode.Clockwise ? 1 : -1));
		}
		Logger.LogTraceFormat(NUDGE, Category.Transform, info, serverState);
		NotifyPlayers();
	}

	/// Dropping with some force, in random direction. For space floating demo purposes.
	[Server]
	public void ForceDrop(Vector3 pos)
	{
		//		GetComponentInChildren<SpriteRenderer>().color = Color.white;
		SetPosition(pos, false);
		Vector2 impulse = Random.insideUnitCircle.normalized;
		//don't apply impulses if item isn't going to float in that direction
		Vector3Int newGoal = CeilWithContext(serverState.WorldPosition, impulse);
		if (MatrixManager.IsNoGravityAt(newGoal, isServer : true))
		{
			serverState.WorldImpulse = impulse;
			serverState.Speed = Random.Range(0.2f, 2f);
		}

		NotifyPlayers();
	}

	///Special rounding for collision detection
	///returns V3Int of next tile
	private static Vector3Int CeilWithContext(Vector3 roundable, Vector2 impulseContext)
	{
		float x = impulseContext.x;
		float y = impulseContext.y;
		return new Vector3Int(
			x < 0 ? (int)Math.Floor(roundable.x) : (int)Math.Ceiling(roundable.x),
			y < 0 ? (int)Math.Floor(roundable.y) : (int)Math.Ceiling(roundable.y),
			0);
	}

	/// <summary>
	/// Server movement checks
	/// </summary>
	/// <returns>true if transform has changed</returns>
	[Server]
	private bool CheckFloatingServer()
	{
		return CheckFloatingServer(TransformState.HiddenPos);
	}

	/// <summary>
	/// internal method, called recursively if more than one tile has passed within one frame
	/// </summary>
	[Server]
	private bool CheckFloatingServer(Vector3 goal)
	{
		bool isRecursive = goal != TransformState.HiddenPos;
		if (!IsFloatingServer || matrix == null)
		{
			return isRecursive;
		}

		Vector3 worldPosition = serverState.WorldPosition;
		Vector3 moveDelta;

		if (!isRecursive)
		{ //Normal delta if not recursive
			moveDelta = (Vector3)serverState.WorldImpulse * serverState.Speed * Time.deltaTime;
		}
		else
		{ //Artificial delta if recursive
			moveDelta = goal - worldPosition;
		}

		Vector3Int intOrigin = Vector3Int.RoundToInt(worldPosition);

		if (intOrigin.x > 18000 || intOrigin.x < -18000 || intOrigin.y > 18000 || intOrigin.y < -18000)
		{
			Stop();
			Logger.Log($"ITEM {transform.name} was forced to stop at {intOrigin}", Category.Movement);
			return true;
		}

		float distance = moveDelta.magnitude;
		Vector3 newGoal;

		if (distance > 1)
		{
			//limit goal to just one tile away and run this method recursively afterwards
			newGoal = worldPosition + (Vector3)serverState.WorldImpulse;
		}
		else
		{
			newGoal = worldPosition + moveDelta;
		}
		Vector3Int intGoal = Vector3Int.RoundToInt(newGoal);

		bool isWithinTile = intOrigin == intGoal; //same tile, no need to validate stuff
		if (isWithinTile || ValidateFloating(worldPosition, newGoal))
		{
			AdvanceMovement(worldPosition, newGoal);
		}
		else
		{
			if (serverState.Speed >= PushPull.HIGH_SPEED_COLLISION_THRESHOLD && IsTileSnap)
			{
				//Stop first (reach tile), then inform about collision
				var collisionInfo = new CollisionInfo
				{
					Speed = serverState.Speed,
						Size = this.Size,
						CollisionTile = intGoal
				};

				Stop();

				OnHighSpeedCollision().Invoke(collisionInfo);
			}
			else
			{
				Stop();
			}
		}

		if (distance > 1)
		{
			CheckFloatingServer(isRecursive ? goal : newGoal);
		}

		return true;
	}

	[Server]
	private void AdvanceMovement(Vector3 tempOrigin, Vector3 tempGoal)
	{
		//Natural throw ending
		if (IsLanding())
		{
			ProcessLandingOnGround();
		}

		serverState.WorldPosition = tempGoal;
		ProcessFloating(tempOrigin);
	}

	private bool IsLanding()
	{
		return IsBeingThrownServer && ShouldStopThrow;
	}

	private void ProcessLandingOnGround()
	{
		OnThrowEnd.Invoke(serverState.ActiveThrow);
		serverState.ActiveThrow = ThrowInfo.NoThrow;
		//Change spin when we hit the ground. Zero was kinda dull
		serverState.SpinFactor = (sbyte) (-serverState.SpinFactor * 0.2f);
		//todo: ground hit sound
	}

	private void ProcessFloating(Vector3 tempOrigin)
	{
		if (CanContinueFloating(tempOrigin)) return;

		//no slide inertia for tile snapped objects like closets
		if (IsTileSnap)
		{
			Stop();
			return;
		}

		ReduceSpeed();
		if (IsSpeedHighEnoughToFloat())
		{
			NotifyPlayers();
		}
		else
		{
			Stop();
		}
	}

	private void ReduceSpeed()
	{
		//on-ground resistance
		serverState.Speed = serverState.Speed - (serverState.Speed * (Time.deltaTime * 10));
	}

	private bool IsSpeedHighEnoughToFloat()
	{
		return serverState.Speed > 0.05f;
	}

	private bool CanContinueFloating(Vector3 tempOrigin)
	{
		var tempOriginInt = Vector3Int.RoundToInt(tempOrigin);
		//Spess drifting is perpetual, but speed decreases each tile if object has landed (no throw) on the floor
		if (IsBeingThrownServer) return true;
		if (MatrixManager.IsSlipperyAt(tempOriginInt) ||
		    MatrixManager.IsNoGravityAt(tempOriginInt, true)) return true;

		return false;
	}

	public static readonly float SpeedHitThreshold = 5f;

	/// Verifies if we can proceed to the next tile and hurts objects if we can not
	/// This check works only for 2 adjacent tiles, that's why floating check is recursive
	[Server]
	private bool ValidateFloating(Vector3 origin, Vector3 goal)
	{
		//		Logger.Log( $"{gameObject.name} check {origin}->{goal}. Speed={serverState.Speed}" );
		var startPosition = Vector3Int.RoundToInt(origin);
		var targetPosition = Vector3Int.RoundToInt(goal);

		var info = serverState.ActiveThrow;
		IReadOnlyCollection<LivingHealthBehaviour> creaturesToHit =
			Vector3Int.RoundToInt(serverState.ActiveThrow.OriginWorldPos) == targetPosition ?
				null : LivingCreaturesInPosition(targetPosition);

		if (serverState.Speed > SpeedHitThreshold)
		{
			OnHit(targetPosition, info, creaturesToHit);
			DamageTile( goal,MatrixManager.GetDamageableTilemapsAt(targetPosition));
		}

		if (CanDriftTo(startPosition, targetPosition, isServer : true))
		{
			//if we can keep drifting and didn't hit anything, keep floating. If we did hit something, only stop if we are impassable (we bonked something),
			//otherwise keep drifting through (we sliced / glanced off them)
			return (creaturesToHit == null || creaturesToHit.Count == 0) ||  (registerTile && registerTile.IsPassable(true));
		}

		return false;
	}

	/// Lists objects to be damaged on given tile. Prob should be moved elsewhere
	private IReadOnlyCollection<LivingHealthBehaviour> LivingCreaturesInPosition(Vector3Int position)
	{
		return MatrixManager.GetAt<LivingHealthBehaviour>(position, isServer: true)?
				.Where(creature =>
					creature.IsDead == false &&
					CanHitObject(creature))
				.ToArray();
	}

	private bool CanHitObject(Component obj)
	{
		var commonTransform = obj.GetComponent<IPushable>();
		if (commonTransform == null) return false;

		if (ServerImpulse.To2Int() == commonTransform.ServerImpulse.To2Int() &&
		    SpeedServer <= commonTransform.SpeedServer)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Hit for thrown (non-tile-snapped) items
	/// </summary>
	private void OnHit(Vector3Int pos, ThrowInfo info, IReadOnlyCollection<LivingHealthBehaviour> hitCreatures)
	{
		if (ItemAttributes == null) return;
		if (hitCreatures == null || hitCreatures.Count <= 0) return;

		foreach (var creature in hitCreatures)
		{
			if(creature.gameObject == info.ThrownBy) continue;
			//Remove cast to int when moving health values to float
			var damage = (int)(ItemAttributes.ServerThrowDamage);
			var hitZone = info.Aim.Randomize();
			creature.ApplyDamageToBodypart(info.ThrownBy, damage, AttackType.Melee, DamageType.Brute, hitZone);
			Chat.AddThrowHitMsgToChat(gameObject,creature.gameObject, hitZone);
			SoundManager.PlayNetworkedAtPos("GenericHit", transform.position, 1f, sourceObj: gameObject);
		}
	}

	/// <summary>
	/// Damages first tile in the list
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="tiles"></param>
	private void DamageTile(Vector3 pos, IReadOnlyList<TilemapDamage> tiles)
	{
		if (ItemAttributes == null) return;
		if (tiles == null || tiles.Count <= 0) return;

		var damage = (int) (ItemAttributes.ServerThrowDamage);
		tiles[0].ApplyDamage(damage, AttackType.Melee, pos);
	}

	/// <Summary>
	/// Use World positions
	/// </Summary>
	private bool CanDriftTo(Vector3Int originPos, Vector3Int targetPos, bool isServer)
	{
		// If we're being thrown, collide like being airborne.

		CollisionType colType =
			(isServer ?
			IsBeingThrownServer : IsBeingThrownClient) ?
					CollisionType.Airborne : CollisionType.Player;

		return MatrixManager.IsPassableAt(originPos, targetPos, isServer, collisionType: colType, includingPlayers : false);
	}
}