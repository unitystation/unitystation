using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NPC;
using UnityEngine;
using UnityEngine.UI;

/// Server only stuff
public class GUI_ShuttleControl : NetTab
{

	private RadarList entryList;
	private RadarList EntryList
	{
		get
		{
			if (!entryList)
			{
				entryList = this["EntryList"] as RadarList;
			}
			return entryList;
		}
	}
	private MatrixMove matrixMove;

	public MatrixMove MatrixMove
	{
		get
		{
			if (!matrixMove)
			{
				matrixMove = Provider.GetComponent<ShuttleConsole>().ShuttleMatrixMove;
			}

			return matrixMove;
		}
	}

	[SerializeField] private Image rcsLight = null;
	[SerializeField] private Sprite rcsLightOn = null;
	[SerializeField] private Sprite rcsLightOff = null;
	[SerializeField] private ToggleButton rcsToggleButton = null;
	private ConnectedPlayer playerControllingRcs;

	public GUI_CoordReadout CoordReadout;

	private GameObject Waypoint;
	Color rulersColor;
	Color rayColor;
	Color crosshairColor;

	public bool RcsMode { get; private set; }


	private NetUIElement<string> SafetyText => (NetUIElement<string>)this[nameof(SafetyText)];
	private NetUIElement<string> StartButton => (NetUIElement<string>)this[nameof(StartButton)];
	private NetColorChanger Crosshair => (NetColorChanger)this[nameof(Crosshair)];
	private NetColorChanger RadarScanRay => (NetColorChanger)this[nameof(RadarScanRay)];
	private NetColorChanger OffOverlay => (NetColorChanger)this[nameof(OffOverlay)];
	private NetColorChanger Rulers => (NetColorChanger)this[nameof(Rulers)];

	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
	}

	private IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
		Trigger = Provider.GetComponent<ShuttleConsole>();
		Trigger.OnStateChange.AddListener(OnStateChange);

		MatrixMove.RegisterCoordReadoutScript(CoordReadout);
		MatrixMove.RegisterShuttleGuiScript(this);

		//Not doing this for clients
		if (IsServer)
		{
			EntryList.Origin = MatrixMove;
			//Init listeners
			string temp = "1";
			StartButton.SetValueServer(temp);
			MatrixMove.MatrixMoveEvents.OnStartMovementServer.AddListener(() => StartButton.SetValueServer("1"));
			MatrixMove.MatrixMoveEvents.OnStopMovementServer.AddListener(() =>
		   {
			   StartButton.SetValueServer("0");
			   HideWaypoint();
		   });

			if (!Waypoint)
			{
				Waypoint = new GameObject($"{MatrixMove.gameObject.name}Waypoint");
			}
			HideWaypoint(false);

			rulersColor = Rulers.Value;
			rayColor =RadarScanRay.Value;
			crosshairColor = Crosshair.Value;

			OnStateChange(State);
		}
		else
		{
			ClientToggleRcs(matrixMove.rcsModeActive);
		}
	}

	private void StartNormalOperation()
	{
		EntryList.AddItems(MapIconType.Ship, GetObjectsOf<MatrixMove>(
			mm => mm != MatrixMove //ignore current ship
			      && (mm.HasWorkingThrusters || mm.gameObject.name.Equals("Escape Pod")) //until pod gets engines
		));

		EntryList.AddItems(MapIconType.Asteroids, GetObjectsOf<Asteroid>());
		var stationBounds = MatrixManager.Get(0).MetaTileMap.GetBounds();
		int stationRadius = (int)Mathf.Abs(stationBounds.center.x - stationBounds.xMin);
		EntryList.AddStaticItem(MapIconType.Station, stationBounds.center, stationRadius);

		EntryList.AddItems(MapIconType.Waypoint, new List<GameObject>(new[] { Waypoint }));

		RescanElements();

		StartRefresh();
	}
	/// <summary>
	/// Add secret emag functionality to radar
	/// </summary>
	private void AddEmagItems()
	{
		EntryList.AddItems( MapIconType.Human, GetObjectsOf<PlayerScript>(player => !player.IsDeadOrGhost) );
		EntryList.AddItems( MapIconType.Ian , GetObjectsOf<NPC.CorgiAI>() );
		EntryList.AddItems( MapIconType.Nuke , GetObjectsOf<Nuke>() );

		RescanElements();

		StartRefresh();
	}

	private bool Autopilot = true;
	public void SetAutopilot(bool on)
	{
		Autopilot = on;
		if (on)
		{
			//touchscreen on
		}
		else {
			//touchscreen off, hide waypoint, invalidate MM target
			HideWaypoint();
			MatrixMove.DisableAutopilotTarget();
		}
	}

	public void ServerToggleRcs(bool on, ConnectedPlayer subject)
	{
		RcsMode = on;
		MatrixMove.ToggleRcs(on);
		SetRcsLight(on);

		if (on)
		{
			playerControllingRcs = subject;
		}
		else
		{
			playerControllingRcs = null;
		}
	}

	public void ClientToggleRcs(bool on)
	{
		RcsMode = on;
		SetRcsLight(on);
		rcsToggleButton.isOn = on;
	}

	private void SetRcsLight(bool on)
	{
		if (on)
		{
			rcsLight.sprite = rcsLightOn;
			return;
		}
		rcsLight.sprite = rcsLightOff;
	}

	public void SetSafetyProtocols(bool on)
	{
		MatrixMove.SafetyProtocolsOn = on;
		SafetyText.SetValueServer(@on ? "ON" : "OFF");
	}


	public void SetWaypoint(string position)
	{
		if (!Autopilot)
		{
			return;
		}
		Vector3 proposedPos = position.Vectorized();
		if (proposedPos == TransformState.HiddenPos)
		{
			return;
		}

		//Ignoring requests to set waypoint outside intended radar window
		if (RadarList.ProjectionMagnitude(proposedPos) > EntryList.Range)
		{
			return;
		}
		//Mind the ship's actual position
		Waypoint.transform.position = (Vector2)proposedPos + Vector2Int.RoundToInt(MatrixMove.ServerState.Position);

		EntryList.UpdateExclusive(Waypoint);

		//		Logger.Log( $"Ordering travel to {Waypoint.transform.position}" );
		MatrixMove.AutopilotTo(Waypoint.transform.position);
	}

	public void HideWaypoint(bool updateImmediately = true)
	{
		Waypoint.transform.position = TransformState.HiddenPos;
		if (updateImmediately)
		{
			EntryList.UpdateExclusive(Waypoint);
		}
	}

	private bool RefreshRadar = false;
	private ShuttleConsole Trigger;
	private TabState State => Trigger.State;

	private void StartRefresh()
	{
		RefreshRadar = true;
		//		Logger.Log( "Starting radar refresh" );
		StartCoroutine(Refresh());
	}

	public void RefreshOnce()
	{
		RefreshRadar = false;
		StartCoroutine(Refresh());
	}

	private void StopRefresh()
	{
		//		Logger.Log( "Stopping radar refresh" );
		RefreshRadar = false;
	}

	/// <summary>
	/// Shut it down as if it has no power. Controls won't react and screen will be empty
	/// </summary>
	private void PowerOff()
	{
		EntryList.Clear();
		StopRefresh();
		TurnOnOff(false);
		RescanElements();
	}

	private IEnumerator Refresh()
	{
		if (State == TabState.Off)
		{
			yield break;
		}
		EntryList.RefreshTrackedPos();
		//Logger.Log((MatrixMove.shuttleFuelSystem.FuelLevel * 100).ToString());
		var fuelGauge = (NetUIElement<string>)this["FuelGauge"];
		if (MatrixMove.ShuttleFuelSystem == null)
		{
			if (fuelGauge.Value != "100") {
				fuelGauge.SetValueServer((100).ToString());
			}

		}
		else {
			fuelGauge.SetValueServer(Math.Round((MatrixMove.ShuttleFuelSystem.FuelLevel * 100)).ToString());
		}
		yield return WaitFor.Seconds(1f);

		//validate Player using Rcs
		if (playerControllingRcs != null)
		{
			bool validate = playerControllingRcs.Script && Validations.CanApply(playerControllingRcs.Script, Provider, NetworkSide.Server);
			if (!validate)
			{
				ServerToggleRcs(false, null);
			}
		}

		if (RefreshRadar)
		{
			StartCoroutine(Refresh());
		}
	}

	/// Get a list of positions for objects of given type within certain range from provided origin
	/// todo: move, make it an util method
	public static List<GameObject> GetObjectsOf<T>(Func<T, bool> condition = null)
		where T : Behaviour
	{
		T[] foundBehaviours = FindObjectsOfType<T>();
		var foundObjects = new List<GameObject>();

		foreach (var foundBehaviour in foundBehaviours)
		{
			if (condition != null && !condition(foundBehaviour))
			{
				continue;
			}

			foundObjects.Add(foundBehaviour.gameObject);
		}

		return foundObjects;
	}


	private void OnStateChange(TabState newState)
	{
		//Important: if you get NREs out of nowhere, make sure your server code doesn't accidentally run on client as well
		if (!IsServer)
		{
			return;
		}

		switch (newState)
		{
			case TabState.Normal:
				PowerOff();
				StartNormalOperation();
				//Important: set values from server using SetValue and not Value
				OffOverlay.SetValueServer(Color.clear);
				Rulers.SetValueServer(rulersColor);
				RadarScanRay.SetValueServer(rayColor);
				Crosshair.SetValueServer(crosshairColor);
				SetSafetyProtocols(true);

				break;
			case TabState.Emagged:
				PowerOff();
				StartNormalOperation();
				//Remove overlay
				OffOverlay.SetValueServer(Color.clear);
				//Repaint radar to evil colours
				Rulers.SetValueServer(HSVUtil.ChangeColorHue(rulersColor, -80));
				RadarScanRay.SetValueServer(HSVUtil.ChangeColorHue(rayColor, -80));
				Crosshair.SetValueServer(HSVUtil.ChangeColorHue(crosshairColor, -80));
				AddEmagItems();
				SetSafetyProtocols(false);

				break;
			case TabState.Off:
				PowerOff();
				//Black screen overlay
				OffOverlay.SetValueServer(Color.black);
				SetSafetyProtocols(true);

				break;
			default:
				return;
		}
	}

	/// <summary>
	/// Starts or stops the shuttle.
	/// </summary>
	/// <param name="off">Toggle parameter</param>
	public void TurnOnOff(bool on)
	{
		if (on && State != TabState.Off)
		{
			MatrixMove.StartMovement();
		}
		else {
			MatrixMove.StopMovement();
		}
	}

	/// <summary>
	/// Turns the shuttle right.
	/// </summary>
	public void TurnRight()
	{
		if (State == TabState.Off)
		{
			return;
		}
		MatrixMove.TryRotate(true);
	}

	/// <summary>
	/// Turns the shuttle left.
	/// </summary>
	public void TurnLeft()
	{
		if (State == TabState.Off)
		{
			return;
		}
		MatrixMove.TryRotate(false);
	}

	/// <summary>
	/// Sets shuttle speed.
	/// </summary>
	/// <param name="speedMultiplier"></param>
	public void SetSpeed(float speedMultiplier)
	{
		if (MatrixMove == null)
		{
			Logger.LogWarning("Matrix move is missing for some reason on this shuttle", Category.Matrix);
			return;
		}
		float speed = speedMultiplier * (MatrixMove.MaxSpeed - 1) + 1;
		//		Logger.Log( $"Multiplier={speedMultiplier}, setting speed to {speed}" );
		MatrixMove.SetSpeed(speed);
	}
}