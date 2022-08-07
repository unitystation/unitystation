using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Shuttles;
using Objects.Command;
using Systems.MobAIs;
using Systems.Shuttles;
using Map;

namespace UI.Objects.Shuttles
{
	public class GUI_ShuttleControl : NetTab
	{
		private RadarList radarList;
		public MatrixMove matrixMove { get; private set; }

		[SerializeField]
		private NetSpriteImage rcsLight = null;

		public GUI_CoordReadout CoordReadout;

		private GameObject Waypoint;
		private Color rulersColor;
		private Color rayColor;
		private Color crosshairColor;

		private NetUIElement<string> SafetyText => (NetUIElement<string>)this[nameof(SafetyText)];
		private NetUIElement<string> StartButton => (NetUIElement<string>)this[nameof(StartButton)];
		private NetColorChanger Crosshair => (NetColorChanger)this[nameof(Crosshair)];
		private NetColorChanger RadarScanRay => (NetColorChanger)this[nameof(RadarScanRay)];
		private NetColorChanger OffOverlay => (NetColorChanger)this[nameof(OffOverlay)];
		private NetColorChanger Rulers => (NetColorChanger)this[nameof(Rulers)];

		private ShuttleFuelSystem shuttleFuelSystem;

		private ShuttleConsole shuttleConsole;

		private bool Autopilot = true;

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
		}

		public void OnDestroy()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			shuttleConsole = Provider.GetComponent<ShuttleConsole>();
			matrixMove = shuttleConsole.ShuttleMatrixMove;
			matrixMove.RegisterCoordReadoutScript(CoordReadout);
			matrixMove.RegisterShuttleGuiScript(this);
			shuttleFuelSystem = matrixMove.ShuttleFuelSystem;
			radarList = this["EntryList"] as RadarList;
			CoordReadout.SetCoords(shuttleConsole.registerTile.Matrix.MatrixMove.transform.position);
			//Not doing this for clients
			if (IsServer)
			{
				shuttleConsole.GUItab = this;

				radarList.Origin = matrixMove;
				StartButton.SetValueServer("1");

				//Init listeners
				matrixMove.MatrixMoveEvents.OnStartMovementServer.AddListener(OnStartMovementServer);
				matrixMove.MatrixMoveEvents.OnStopMovementServer.AddListener(OnStopMovementServer);

				if (!Waypoint)
				{
					Waypoint = new GameObject($"{matrixMove.gameObject.name}Waypoint");
				}
				HideWaypoint(false);

				rulersColor = Rulers.Value;
				rayColor = RadarScanRay.Value;
				crosshairColor = Crosshair.Value;

				OnStateChange(shuttleConsole.shuttleConsoleState);
			}
		}

		private void UpdateMe()
		{
			radarList.RefreshTrackedPos();
			CoordReadout.SetCoords(shuttleConsole.registerTile.Matrix.MatrixMove.transform.position);

			var fuelGauge = (NetUIElement<string>)this["FuelGauge"];
			if (shuttleFuelSystem == null)
			{
				if (fuelGauge.Value != "0")
				{
					fuelGauge.SetValueServer((0).ToString());
				}
			}
			else if(shuttleFuelSystem.Connector?.canister?.GasContainer != null)
			{
				var value = $"{(shuttleFuelSystem.FuelLevel * 100f)}";
				fuelGauge.SetValueServer(value);
			}

			if (matrixMove.rcsModeActive)
			{
				if (Validations.CanApply(matrixMove.playerControllingRcs, Provider, NetworkSide.Server) == false)
				{
					shuttleConsole.ChangeRcsPlayer(false, matrixMove.playerControllingRcs);
				}
			}
		}

		public void OnStateChange(ShuttleConsoleState newState)
		{
			if (newState == ShuttleConsoleState.Off)
			{
				SetSafetyProtocols(true);
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
				ClearScreen();
				return;
			}
			if (newState == ShuttleConsoleState.Normal)
			{
				AddRadarItems();
				//Important: set values from server using SetValue and not Value
				Rulers.SetValueServer(rulersColor);
				RadarScanRay.SetValueServer(rayColor);
				Crosshair.SetValueServer(crosshairColor);
				SetSafetyProtocols(true);
			}
			else if (newState == ShuttleConsoleState.Emagged)
			{
				AddRadarItems(true);
				//Repaint radar to evil colours
				Rulers.SetValueServer(HSVUtil.ChangeColorHue(rulersColor, -80));
				RadarScanRay.SetValueServer(HSVUtil.ChangeColorHue(rayColor, -80));
				Crosshair.SetValueServer(HSVUtil.ChangeColorHue(crosshairColor, -80));
				SetSafetyProtocols(false);
			}
			UpdateManager.Add(UpdateMe, 1f);
			OffOverlay.SetValueServer(Color.clear);
		}

		private void ClearScreen()
		{
			//Black screen overlay
			OffOverlay.SetValueServer(Color.black);
			radarList.Clear();
			ToggleEngine(false);
			shuttleConsole.ChangeRcsPlayer(false, matrixMove.playerControllingRcs);
		}

		private void AddRadarItems(bool emagged = false)
		{
			radarList.AddItems(MapIconType.Ship, GetObjectsOf<MatrixMove>(
				mm => mm != matrixMove //ignore current ship
				      && (mm.HasWorkingThrusters || mm.gameObject.name.Equals("Escape Pod")) //until pod gets engines
			));

			radarList.AddItems(MapIconType.Asteroids, GetObjectsOf<Asteroid>());
			var stationBounds = MatrixManager.MainStationMatrix.MetaTileMap.GetLocalBounds();
			var stationRadius = (int) Mathf.Abs(stationBounds.center.x - stationBounds.xMin);
			radarList.AddStaticItem(MapIconType.Station, stationBounds.center.RoundTo2Int(), stationRadius);
			radarList.AddItems(MapIconType.Waypoint, new List<GameObject>(new[] {Waypoint}));

			if (emagged)
			{
				radarList.AddItems(MapIconType.Human, GetObjectsOf<PlayerScript>(player => !player.IsDeadOrGhost));
				radarList.AddItems(MapIconType.Ian, GetObjectsOf<CorgiAI>());
				radarList.AddItems(MapIconType.Nuke, GetObjectsOf<Nuke>());
			}
		}

		/// Get a list of positions for objects of given type within certain range from provided origin
		/// todo: move, make it an util method
		public static List<GameObject> GetObjectsOf<T>(Func<T, bool> condition = null) where T : Behaviour
		{
			var foundBehaviours = FindObjectsOfType<T>();
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

		private void SetSafetyProtocols(bool state)
		{
			matrixMove.SafetyProtocolsOn = state;
			SafetyText.SetValueServer(state ? "ON" : "OFF");
		}

		private void OnStopMovementServer()
		{
			StartButton.SetValueServer("0");
			HideWaypoint();
		}

		private void OnStartMovementServer()
		{
			// dont enable button when moving with RCS
			if (!matrixMove.rcsModeActive)
			{
				StartButton.SetValueServer("1");
			}
		}

		public void ToggleAutopilot(bool on)
		{
			Autopilot = on;
			if (on)
			{
				//touchscreen on
			}
			else
			{
				//touchscreen off, hide waypoint, invalidate MM target
				HideWaypoint();
				matrixMove.DisableAutopilotTarget();
			}
		}

		public void ToggleRcsButton(PlayerInfo connectedPlayer)
		{
			if (matrixMove.playerControllingRcs != null && matrixMove.playerControllingRcs != connectedPlayer.Script)
			{
				shuttleConsole.ChangeRcsPlayer(false, matrixMove.playerControllingRcs);
			}
			var newState = !matrixMove.rcsModeActive;
			shuttleConsole.ChangeRcsPlayer(newState, connectedPlayer.Script);
		}

		public void SetRcsLight(bool state)
		{
			rcsLight.SetSprite(state ? 1 : 0);
		}

		public void SetWaypoint(string position)
		{
			if (!Autopilot) return;

			Vector3 proposedPos = position.Vectorized();
			if (proposedPos == TransformState.HiddenPos) return;

			//Ignoring requests to set waypoint outside intended radar window
			if (RadarList.ProjectionMagnitude(proposedPos) > radarList.Range) return;

			//Mind the ship's actual position
			Waypoint.transform.position = (Vector2)proposedPos + Vector2Int.RoundToInt(matrixMove.ServerState.Position);

			radarList.UpdateExclusive(Waypoint);

			matrixMove.AutopilotTo(Waypoint.transform.position);
		}

		private void HideWaypoint(bool updateImmediately = true)
		{
			Waypoint.transform.position = TransformState.HiddenPos;
			if (updateImmediately)
			{
				radarList.UpdateExclusive(Waypoint);
			}
		}

		/// <summary>
		/// Starts or stops the shuttle.
		/// </summary>
		/// <param name="off">Toggle parameter</param>
		public void ToggleEngine(bool engineState)
		{
			if (engineState && shuttleConsole.shuttleConsoleState != ShuttleConsoleState.Off && !matrixMove.rcsModeActive)
			{
				matrixMove.StartMovement();
			}
			else
			{
				matrixMove.StopMovement();
			}
		}

		/// <summary>
		/// Turns the shuttle right.
		/// </summary>
		public void TurnRight()
		{
			if (shuttleConsole.shuttleConsoleState == ShuttleConsoleState.Off) return;

			matrixMove.TryRotate(true);
		}

		/// <summary>
		/// Turns the shuttle left.
		/// </summary>
		public void TurnLeft()
		{
			if (shuttleConsole.shuttleConsoleState == ShuttleConsoleState.Off) return;

			matrixMove.TryRotate(false);
		}

		/// <summary>
		/// Sets shuttle speed.
		/// </summary>
		/// <param name="speedMultiplier"></param>
		public void SetSpeed(float speedMultiplier)
		{
			var speed = speedMultiplier * (matrixMove.MaxSpeed - 1) + 1;
			matrixMove.SetSpeed(speed);
		}

		public void PlayRadarDetectionSound()
		{
			shuttleConsole.PlayRadarDetectionSound();
		}
	}
}
