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

	[Tooltip("Does it require fuel in order to fly?")]
	public bool RequiresFuel;

	/// <summary>
	/// Initial facing of the ship as mapped in the editor.
	/// </summary>
	public Orientation InitialFacing => Orientation.FromEnum(initialFacing);

	[Tooltip("Max flying speed of this matrix.")]
	[FormerlySerializedAs("maxSpeed")]
	public float MaxSpeed = 20f;

	[SyncVar][Tooltip("Whether safety is currently on, preventing collisions when sensors detect them.")]
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
	public RotationOffset FacingOffsetFromInitial => ServerState.FacingOffsetFromInitial(this);

	/// <summary>
	/// If it is currently fuelled
	/// </summary>
	[NonSerialized]
	public bool IsFueled;

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

	public override void FixedUpdateMe()
	{
		if (isServer)
		{
			CheckMovementServer();
		}
		else
		{
			CheckMovementClient();
		}
	//	AnimateMovement();
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
			if(coordReadoutScript != null) coordReadoutScript.SetCoords(ServerState.Position);
			if (shuttleControlGUI != null && rcsModeActive != shuttleControlGUI.RcsMode)
			{
				shuttleControlGUI.ClientToggleRcs(rcsModeActive);
			}
		}
	}

	private bool CanMoveTo(Orientation direction)
	{
		Vector3 dir = direction.Vector;

		//		check if next tile is passable
		for (var i = 0; i < SensorPositions.Length; i++)
		{
			var sensor = SensorPositions[i];
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(sensor, matrixInfo, ServerState);

			// Exclude the moving matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos + dir.RoundToInt(), isServer: true,
											collisionType: matrixColliderType, excludeList: excludeList))
			{
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
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(localSensorAggrigateVector, matrixInfo, ServerState);

			// Exclude the rotating matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos, isServer: true,
											collisionType: matrixColliderType, includingPlayers: true, excludeList: excludeList))
			{
				return false;
			}
		}

		return true;
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

			DebugGizmoUtils.DrawArrow(transform.position, ServerState.FlyingDirection.Vector*2);
			return;
		}

		//serverState
		Gizmos.color = color1;
		Vector3 serverPos = ServerState.Position;
		Gizmos.DrawWireCube(serverPos, size1);
		if (ServerState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(serverPos + Vector3.right / 3, ServerState.FlyingDirection.Vector * ServerState.Speed);
			DebugGizmoUtils.DrawText(ServerState.Speed.ToString(), serverPos + Vector3.right, 15);
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