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
using Logs;
using UnityEngine.UI;

namespace UI.Objects.Shuttles
{
	public class GUI_ShuttleControl : NetTab
	{
		public MatrixMove matrixMove { get; private set; }

		[SerializeField] private NetSpriteImage rcsLight = null;

		public GUI_CoordReadout CoordReadout;

		private NetUIElement<string> SafetyText => (NetUIElement<string>) this[nameof(SafetyText)];
		public NetUIElement<string> StartButton => (NetUIElement<string>) this[nameof(StartButton)];
		private NetColorChanger OffOverlay => (NetColorChanger) this[nameof(OffOverlay)];

		private ShuttleConsole shuttleConsole;

		public NetSlider GoodZoomSlider;

		public NetSlider EngineSlider;

		private bool Autopilot = true;

		public Image Preview;

		public NetToggle ReverseButton;

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(WaitForProvider());
			if (this.IsMasterTab == false)
			{
				UpdateManager.Add(CallbackType.UPDATE, ClientNonMasterUpdate);
			}
		}


		public void OnDisable()
		{
			if (this.IsMasterTab == false)
			{
				UpdateManager.Remove(CallbackType.UPDATE, ClientNonMasterUpdate);
			}
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

			Preview.sprite = ShuttleCameraRenderer.UISprite;
			shuttleConsole = Provider.GetComponent<ShuttleConsole>();
			matrixMove = shuttleConsole.ShuttleMatrixMove;
			CoordReadout.SetCoords(shuttleConsole.registerTile.Matrix.MatrixMove.transform.position);
			//Not doing this for clients
			if (IsMasterTab)
			{
				shuttleConsole.GUItab = this;

				//Init listeners
				matrixMove.NetworkedMatrixMove.OnStartMovement.AddListener(OnStartMovementServer); ;
				OnStateChange(shuttleConsole.shuttleConsoleState);
			}
		}

		private void ClientNonMasterUpdate()
		{

			var Value = GoodZoomSlider.Element.value;

			if (Value > 0.33f == false)
			{
				ShuttleCameraRenderer.instance.renderCamera.orthographicSize = 100f;
			}
			else if (Value > 0.66f == false)
			{
				ShuttleCameraRenderer.instance.renderCamera.orthographicSize = 50f;
			}
			else
			{
				ShuttleCameraRenderer.instance.renderCamera.orthographicSize = 25f;
			}
		}

		private void UpdateMe()
		{
			CoordReadout.SetCoords(shuttleConsole.registerTile.Matrix.MatrixMove.transform.position);

			var fuelGauge = (NetUIElement<string>) this["FuelGauge"];
			// if (shuttleFuelSystem == null)
			// {
			// 	if (fuelGauge.Value != "0")
			// 	{
			// 		fuelGauge.MasterSetValue((0).ToString());
			// 	}
			// }
			// else if (shuttleFuelSystem.Connector?.canister?.GasContainer != null)
			// {
			// 	var value = $"{(shuttleFuelSystem.FuelLevel * 100f)}";
			// 	fuelGauge.MasterSetValue(value);
			// }

			if (matrixMove.NetworkedMatrixMove.RCSModeActive)
			{
				if (Validations.CanApply(matrixMove.NetworkedMatrixMove.playerControllingRcs, Provider, NetworkSide.Server) == false)
				{
					shuttleConsole.ChangeRcsPlayer(false, matrixMove.NetworkedMatrixMove.playerControllingRcs);
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
				SetSafetyProtocols(true);
			}
			else if (newState == ShuttleConsoleState.Emagged)
			{
				SetSafetyProtocols(false);
			}

			UpdateManager.Add(UpdateMe, 1f);
			//OffOverlay.MasterSetValue(Color.clear);
		}

		private void ClearScreen()
		{
			//Black screen overlay
			//OffOverlay.MasterSetValue(Color.black);
			ToggleEngine(false);
			shuttleConsole.ChangeRcsPlayer(false, matrixMove.NetworkedMatrixMove.playerControllingRcs);
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
			//matrixMove.SafetyProtocolsOn = state;
			SafetyText.MasterSetValue(state ? "ON" : "OFF");
		}


		private void OnStartMovementServer()
		{
			 //dont enable button when moving with RCS
			if (!matrixMove.NetworkedMatrixMove.RCSModeActive)
			{
				StartButton.MasterSetValue("1");
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
				//matrixMove.DisableAutopilotTarget();
			}
		}

		public void ToggleRcsButton(PlayerInfo connectedPlayer)
		{
			if (matrixMove.NetworkedMatrixMove.playerControllingRcs != null && matrixMove.NetworkedMatrixMove.playerControllingRcs != connectedPlayer.Script)
			{
				shuttleConsole.ChangeRcsPlayer(false, matrixMove.NetworkedMatrixMove.playerControllingRcs);
			}

			var newState = !matrixMove.NetworkedMatrixMove.RCSModeActive;
			shuttleConsole.ChangeRcsPlayer(newState, connectedPlayer.Script);
		}

		public void SetRcsLight(bool state)
		{
			rcsLight.SetSprite(state ? 1 : 0);
		}

		public void ToggleReverse(bool Reverse)
		{
			SetSpeed(EngineSlider.Element.value);
		}



		/// <summary>
		/// Starts or stops the shuttle.
		/// </summary>
		/// <param name="off">Toggle parameter</param>
		public void ToggleEngine(bool engineState)
		{
			if (engineState && shuttleConsole.shuttleConsoleState != ShuttleConsoleState.Off && !matrixMove.NetworkedMatrixMove.RCSModeActive)
			{

			}
			else
			{
				matrixMove.NetworkedMatrixMove.TurnOffAllThrusters();
			}
		}

		/// <summary>
		/// Turns the shuttle right.
		/// </summary>
		public void TurnRight()
		{
			if (shuttleConsole.shuttleConsoleState == ShuttleConsoleState.Off) return;

			if (StartButton.Value == "0") return;

			matrixMove.NetworkedMatrixMove.SetThrusterStrength(Thruster.ThrusterDirectionClassification.Right ,1);
		}

		/// <summary>
		/// Turns the shuttle left.
		/// </summary>
		public void TurnLeft()
		{
			if (shuttleConsole.shuttleConsoleState == ShuttleConsoleState.Off) return;
			if (StartButton.Value == "0") return;

			matrixMove.NetworkedMatrixMove.SetThrusterStrength(Thruster.ThrusterDirectionClassification.Left ,1);
		}

		public void SetLeftAndRightThrusters(float LeftAndRightMultiplier)
		{
			if (StartButton.Value == "0") return;
			if (LeftAndRightMultiplier is < 95 and > 85)
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Right,  0);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Left,  0);
				return;
			}
			else if (LeftAndRightMultiplier > 95)
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Right,  0);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Left ,(LeftAndRightMultiplier - 90f) / 90f);
			}
			else
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Left,  0);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Right ,(90f - LeftAndRightMultiplier) / 90f);
			}

		}


		/// <summary>
		/// Sets shuttle speed.
		/// </summary>
		/// <param name="speedMultiplier"></param>
		public void SetSpeed(float speedMultiplier)
		{
			if (StartButton.Value == "0") return;
			if (ReverseButton.Value == "1")
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Backwards ,speedMultiplier);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Forwards ,0);
			}
			else
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Forwards ,speedMultiplier);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Backwards ,0);
			}

		}

		public void PlayRadarDetectionSound()
		{
			shuttleConsole.PlayRadarDetectionSound();
		}
	}
}