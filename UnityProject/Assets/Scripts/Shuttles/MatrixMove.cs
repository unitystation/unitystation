using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
/// Behavior which allows an entire matrix to move and rotate (and be synced over the network).
/// This behavior must go on a gameobject that is the parent of the gameobject that has the actual Matrix component.
/// </summary>
public partial class MatrixMove : ManagedNetworkBehaviour, IPlayerControllable
{
	public bool debug = false;
	/// <summary>
	/// Set this to make sure collisions are correct for the MatrixMove
	/// For example, shuttles collide with floors but players don't
	/// </summary>
	public CollisionType matrixColliderType = CollisionType.Shuttle;

	/// <summary>
	/// If anything has a specific UI that needs to be set, it can change based off this var
	/// </summary>
	public UIType uiType = UIType.Nanotrasen;

	[Tooltip("Initial facing of the ship. Very important to set this correctly!")]
	[SerializeField]
	private OrientationEnum initialFacing;
	/// <summary>
	/// Initial facing of the ship as mapped in the editor.
	/// </summary>
	public Orientation InitialFacing => Orientation.FromEnum(initialFacing);

	[Tooltip("Max flying speed of this matrix.")]
	[FormerlySerializedAs("maxSpeed")]
	public float MaxSpeed = 20f;

	[Tooltip("Whether safety is currently on, preventing collisions when sensors detect them.")]
	public bool SafetyProtocolsOn = true;


	[SyncVar(hook = nameof(SyncInitialPosition))]
	private Vector3 initialPosition;
	/// <summary>
	/// Initial position for offset calculation, set on start and never changed afterwards
	/// </summary>
	public Vector3Int InitialPosition => initialPosition.RoundToInt();

	[SyncVar(hook = nameof(SyncPivot))]
	private Vector3 pivot;
	/// <summary>
	/// local pivot point, set on start and never changed afterwards
	/// </summary>
	public Vector3Int Pivot => pivot.RoundToInt();

	/// <summary>
	/// All the various events that can be subscribed to on this matrix
	/// </summary>
	public readonly MatrixMoveEvents MatrixMoveEvents = new MatrixMoveEvents();

	/// <summary>
	/// Gets the rotation offset this matrix has from its initial mapped
	/// facing.
	/// </summary>
	public RotationOffset FacingOffsetFromInitial => ClientState.FacingOffsetFromInitial(this);

	/// <summary>
	/// If it is currently fuelled
	/// </summary>
	[NonSerialized]
	public bool IsFueled;

	[Tooltip("Does it require fuel in order to fly?")]
	public bool RequiresFuel;

	private List<RcsThruster> bowRcsThrusters = new List<RcsThruster>(); //front
	private List<RcsThruster> sternRcsThrusters = new List<RcsThruster>(); //back
	private List<RcsThruster> portRcsThrusters = new List<RcsThruster>(); //left
	private List<RcsThruster> starBoardRcsThrusters = new List<RcsThruster>(); //right
	public ConnectedPlayer playerControllingRcs { get; private set; }

	[SyncVar] [HideInInspector]
	public bool rcsModeActive;

	private bool IsAutopilotEngaged => Target != TransformState.HiddenPos;

	private MatrixInfo matrixInfo;
	public MatrixInfo MatrixInfo => matrixInfo;
	private ShuttleFuelSystem shuttleFuelSystem;
	public ShuttleFuelSystem ShuttleFuelSystem => shuttleFuelSystem;

	private MatrixPositionFilter matrixPositionFilter = new MatrixPositionFilter();

	private Coroutine floatingSyncHandle;

	private List<ShipThruster> thrusters = new List<ShipThruster>();
	public bool HasWorkingThrusters => thrusters.Count > 0;

	private Vector3Int[] SensorPositions;
	private GameObject[] RotationSensors;
	private GameObject rotationSensorContainerObject;
	/// <summary>
	/// Tracks the rotation we are currently performing.
	/// Null when a rotation is not in progress.
	/// NOTE: This is not an offset from initialfacing, it's an offset from our current facing. So
	/// if we are turning 90 degrees right, this will be Right no matter what our initial conditions were.
	/// </summary>
	private RotationOffset? inProgressRotation;

	private readonly int rotTime = 90;
	[HideInInspector]
	private GUI_CoordReadout coordReadoutScript;

	private GUI_ShuttleControl shuttleControlGUI;
	private int moveCur = -1;
	private int moveLimit = -1;

	private void RecheckThrusters()
	{
		thrusters = GetComponentsInChildren<ShipThruster>(true).ToList();
	}

	public void RegisterShuttleFuelSystem(ShuttleFuelSystem shuttleFuel)
	{
		this.shuttleFuelSystem = shuttleFuel;
	}

	public void RegisterShuttleGuiScript(GUI_ShuttleControl shuttleGui)
	{
		shuttleControlGUI = shuttleGui;
	}
	public void RegisterCoordReadoutScript(GUI_CoordReadout coordReadout)
	{
		coordReadoutScript = coordReadout;
	}

	///managed by UpdateManager
	public override void FixedUpdateMe()
	{
		if (isServer)
		{
			CheckMovementServer();
		}
	}

	public override void UpdateMe()
	{
		AnimateMovement();
	}

	///managed by UpdateManager
	public override void LateUpdateMe()
	{
		//finish rotation now that the transform should finally be rotated
		if (!NeedsRotationClient && inProgressRotation != null)
		{
			//client and server logic happens here because server also must wait for the rotation to finish lerping.
			Logger.LogTraceFormat("{0} ending rotation progress to {1}", Category.Matrix, this, inProgressRotation.Value);
			if (isServer)
			{
				MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Server, RotationEvent.End));
			}
			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, RotationEvent.End));
			inProgressRotation = null;
			if (pendingInitialRotation && !receivedInitialState)
			{
				receivedInitialState = true;
				pendingInitialRotation = false;
			}
		}

		if (isClient)
		{
			if(coordReadoutScript != null) coordReadoutScript.SetCoords(clientState.Position);
			if (shuttleControlGUI != null && rcsModeActive != shuttleControlGUI.RcsMode)
			{
				shuttleControlGUI.ClientToggleRcs(rcsModeActive);
			}
		}
	}

	/// <summary>
	/// Performs the rotation / movement animation on all clients and server. Called every UpdateMe()
	/// </summary>
	private void AnimateMovement()
	{
		if (Equals(clientState, MatrixState.Invalid))
		{
			return;
		}

		if (NeedsRotationClient)
		{
			//rotate our transform to our new facing direction
			if (clientState.RotationTime != 0)
			{
				//animate rotation
				transform.rotation =
					Quaternion.RotateTowards(transform.rotation,
						 InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion,
						Time.deltaTime * clientState.RotationTime);
			}
			else
			{
				//rotate instantly
				transform.rotation = InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion;
			}
		}
		else if (IsMovingClient)
		{
			//Only move target if rotation is finished
			//predict client state because we don't get constant updates when flying in one direction.
			clientState.Position += (clientState.Speed * Time.deltaTime) * clientState.FlyingDirection.Vector;
		}

		//finish rotation (rotation event will be fired in lateupdate
		if (!NeedsRotationClient && inProgressRotation != null)
		{
			// Finishes the job of Lerp and straightens the ship with exact angle value
			transform.rotation = InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion;
		}

		//Lerp
		if (clientState.Position != transform.position)
		{
			float distance = Vector3.Distance(clientState.Position, transform.position);

			//Teleport (Greater then 30 unity meters away from server target):
			if (distance > 30f)
			{
				matrixPositionFilter.FilterPosition(transform, clientState.Position, clientState.FlyingDirection);
				return;
			}

			transform.position = clientState.Position;

			//If stopped then lerp to target (snap to grid)
			if (!clientState.IsMoving )
			{
				if ( clientState.Position == transform.position )
				{
					MatrixMoveEvents.OnFullStopClient.Invoke();
				}
				if ( distance > 0f )
				{
					//TODO: Why is this needed? Seems weird.
					matrixPositionFilter.SetPosition(transform.position);
					return;
				}
			}

			matrixPositionFilter.FilterPosition(transform, transform.position, clientState.FlyingDirection);
		}
	}

	private bool CanMoveTo(Orientation direction)
	{
		Vector3 dir = direction.Vector;

		//		check if next tile is passable
		for (var i = 0; i < SensorPositions.Length; i++)
		{
			var sensor = SensorPositions[i];
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(sensor, matrixInfo, serverTargetState);

			// Exclude the moving matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos + dir.RoundToInt(), isServer: true,
											collisionType: matrixColliderType, excludeList: excludeList))
			{
				Logger.LogTrace(
					$"Can't pass {serverTargetState.Position}->{serverTargetState.Position + dir} (because {sensorPos}->{sensorPos + dir})!",
					Category.Matrix);
				return false;
			}
		}

//		Logger.LogTrace( $"Passing {serverTargetState.Position}->{serverTargetState.Position+dir} ", Category.Matrix );
		return true;
	}

	private bool CanRotateTo(Orientation flyingDirection)
	{
		if (rotationSensorContainerObject == null) { return true; }

		// Feign a rotation using GameObjects for reference
		Transform rotationSensorContainerTransform = rotationSensorContainerObject.transform;
		rotationSensorContainerTransform.rotation = new Quaternion();
		rotationSensorContainerTransform.Rotate(0f, 0f, 90f * ServerState.FlyingDirection.RotationsTo(flyingDirection));

		for (var i = 0; i < RotationSensors.Length; i++)
		{
			var sensor = RotationSensors[i];
			// Need to pass an aggriate local vector in reference to the Matrix GO to get the correct WorldPos
			Vector3 localSensorAggrigateVector = (rotationSensorContainerTransform.localRotation * sensor.transform.localPosition) + rotationSensorContainerTransform.localPosition;
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(localSensorAggrigateVector, matrixInfo, serverTargetState);

			// Exclude the rotating matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos, isServer: true,
											collisionType: matrixColliderType, includingPlayers: true, excludeList: excludeList))
			{
				Logger.LogTrace(
					$"Can't rotate at {serverTargetState.Position}->{serverTargetState.Position } (because {sensorPos} is occupied)!",
					Category.Matrix);
				return false;
			}
		}

		return true;
	}

	//Searches the matrix for RcsThrusters
	public void CacheRcs()
	{
		ClearRcsCache();
		foreach(Transform t in matrixInfo.Objects)
		{
			if (t.tag.Equals("Rcs"))
			{
				CacheRcs(t.GetComponent<DirectionalRotatesParent>().MappedOrientation,
					t.GetComponent<RcsThruster>());
			}
		}
	}

	void CacheRcs(OrientationEnum mappedOrientation, RcsThruster thruster)
	{
		if (InitialFacing == Orientation.Up)
		{
			if(mappedOrientation == OrientationEnum.Up) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) starBoardRcsThrusters.Add(thruster);
		}

		if (InitialFacing == Orientation.Right)
		{
			if(mappedOrientation == OrientationEnum.Up) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) bowRcsThrusters.Add(thruster);
		}

		if (InitialFacing == Orientation.Down)
		{
			if(mappedOrientation == OrientationEnum.Up) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) portRcsThrusters.Add(thruster);
		}

		if (InitialFacing == Orientation.Left)
		{
			if(mappedOrientation == OrientationEnum.Up) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) sternRcsThrusters.Add(thruster);
		}
	}

	void ClearRcsCache()
	{
		bowRcsThrusters.Clear();
		sternRcsThrusters.Clear();
		portRcsThrusters.Clear();
		starBoardRcsThrusters.Clear();
	}

#if UNITY_EDITOR
	//Visual debug
	private Vector3 size1 = Vector3.one;
	private Vector3 size2 = new Vector3(0.9f, 0.9f, 0.9f);
	private Vector3 size3 = new Vector3(0.8f, 0.8f, 0.8f);
	private Color color1 = Color.red;
	private Color color2 = DebugTools.HexToColor("81a2c7");
	private Color color3 = Color.white;

	private void OnDrawGizmos()
	{
		if ( !Application.isPlaying )
		{ //Showing matrix pivot if game is stopped
			Gizmos.color = color1.WithAlpha( 0.6f );
			Gizmos.DrawCube(transform.position, Vector3.one );
			Gizmos.color = color1;
			Gizmos.DrawWireCube(transform.position, Vector3.one );

			DebugGizmoUtils.DrawArrow(transform.position, clientState.FlyingDirection.Vector*2);
			return;
		}

		//serverState
		Gizmos.color = color1;
		Vector3 serverPos = serverState.Position;
		Gizmos.DrawWireCube(serverPos, size1);
		if (serverState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(serverPos + Vector3.right / 3, serverState.FlyingDirection.Vector * serverState.Speed);
			DebugGizmoUtils.DrawText(serverState.Speed.ToString(), serverPos + Vector3.right, 15);
		}

		//serverTargetState
		Gizmos.color = color2;
		Vector3 serverTargetPos = serverTargetState.Position;
		Gizmos.DrawWireCube(serverTargetPos, size2);
		if (serverTargetState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(serverTargetPos, serverTargetState.FlyingDirection.Vector * serverTargetState.Speed);
			DebugGizmoUtils.DrawText(serverTargetState.Speed.ToString(), serverTargetPos + Vector3.down, 15);
		}

		//clientState
		Gizmos.color = color3;
		Vector3 pos = clientState.Position;
		Gizmos.DrawWireCube(pos, size3);
		if (clientState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(pos + Vector3.left / 3, clientState.FlyingDirection.Vector * clientState.Speed);
			DebugGizmoUtils.DrawText(clientState.Speed.ToString(), pos + Vector3.left, 15);
		}
	}
#endif
}

public enum UIType
{
	Default = 0,
	Nanotrasen = 1,
	Syndicate = 2
};