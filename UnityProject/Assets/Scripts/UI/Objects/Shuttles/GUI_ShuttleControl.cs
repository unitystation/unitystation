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
		public bool StartButton => shuttleConsole.EngineOn;
		public NetUIElement<string> GUIStartButton => (NetUIElement<string>) this["StartButton"];

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

			ShuttleCameraRenderer.instance.UpdateME();
		}

		private void UpdateMe()
		{
			if (shuttleConsole == null)
			{
				Destroy(this.gameObject);
				return;
			}
			CoordReadout.SetCoords(shuttleConsole.transform.position);

			var fuelGauge = (NetUIElement<string>) this["FuelGauge"];

			if (matrixMove.NetworkedMatrixMove.ConnectedThrusters.Count > 0)
			{
				if (matrixMove.NetworkedMatrixMove.ConnectedThrusters[0].pipeData.SelfSufficient)
				{
					var value = $"{(1 * 100f)}";
					fuelGauge.MasterSetValue(value);
				}
				else
				{
					var value = $"{( Math.Min((matrixMove.NetworkedMatrixMove.ConnectedThrusters[0].InletPressure / 2500f), 1) * 100f)}";
					fuelGauge.MasterSetValue(value);
				}

			}



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
			matrixMove.NetworkedMatrixMove.Safety = state;
			SafetyText.MasterSetValue(state ? "ON" : "OFF");
		}


		public void ToggleAutopilot(bool on)
		{
			Autopilot = on;
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


		public void ToggleEngineSupport(bool EngineSupport)
		{
			shuttleConsole.EngineSupport = EngineSupport;
		}


		/// <summary>
		/// Starts or stops the shuttle.
		/// </summary>
		/// <param name="off">Toggle parameter</param>
		public void ToggleEngine(bool engineState)
		{
			shuttleConsole.EngineOn = engineState;
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

			if (shuttleConsole.EngineOn == false) return;

			matrixMove.NetworkedMatrixMove.SetThrusterStrength(Thruster.ThrusterDirectionClassification.Right ,1, true);
		}

		/// <summary>
		/// Turns the shuttle left.
		/// </summary>
		public void TurnLeft()
		{
			if (shuttleConsole.shuttleConsoleState == ShuttleConsoleState.Off) return;
			if (shuttleConsole.EngineOn == false) return;

			matrixMove.NetworkedMatrixMove.SetThrusterStrength(Thruster.ThrusterDirectionClassification.Left ,1, true);
		}

		public void SetLeftAndRightThrusters(float LeftAndRightMultiplier)
		{
			if (shuttleConsole.EngineOn == false) return;
			if (LeftAndRightMultiplier is < 95 and > 85)
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Right,  0, true);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Left,  0, true);
				return;
			}
			else if (LeftAndRightMultiplier > 95)
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Right,  0, true);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Left ,(LeftAndRightMultiplier - 90f) / 90, true);
			}
			else
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Left,  0, true);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Right ,(90f - LeftAndRightMultiplier) / 90f, true);
			}

		}


		/// <summary>
		/// Sets shuttle speed.
		/// </summary>
		/// <param name="speedMultiplier"></param>
		public void SetSpeed(float speedMultiplier)
		{
			if (shuttleConsole.EngineOn == false) return;
			if (ReverseButton.Value == "1")
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Down ,speedMultiplier, true);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Up ,0, true);
			}
			else
			{
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Up ,speedMultiplier, true);
				matrixMove.NetworkedMatrixMove.SetThrusterStrength( Thruster.ThrusterDirectionClassification.Down ,0, true);
			}

		}

		public void PlayRadarDetectionSound()
		{
			shuttleConsole.PlayRadarDetectionSound();
		}
	}
}