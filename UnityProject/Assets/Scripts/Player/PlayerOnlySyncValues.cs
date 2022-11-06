using System;
using CameraEffects;
using Core.Utils;
using Items;
using Items.Tool;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Player
{
	//how TO use
	//so, First of all you need 2 fields
	//the default state e.g when you ghost what it should be set to
	//and the active synchronised state
	//you got these two down now you build the hook
	//private void SyncXrayState(bool NotSetValueServer, bool newState)
	//so you Use SyncXrayState  to set the value, Noting that NotSetValueServer = False, every time you call it
	//and then inside of this statement
	//
	//if ((NotSetValueServer == false && isServer))
	//{
	//	SyncXRay = newState;
	//}
	//This is required so local client doesn't get Buggered by values getting reset when they Ghost
	//And then you do this check
	//if (ClientForThisBody)
	//This goes around your code that applies the value such as fov mask = x
	//this is to allow is local player = false in cases where it needs to be reset to default
	//You also must use the past in value
	// Such as NightVisionGoggles.NightVisionData newState
	// because the synchronised value is the non-reset one
	//Also use the Sync Your thing function with false and value


	/// <summary>
	/// Class which contains sync vars which are only sent to the client controlling this player
	/// </summary>
	public class PlayerOnlySyncValues : NetworkBehaviour, IClientPlayerLeaveBody, IClientPlayerTransferProcess
	{




		#region Defaults
		private NightVisionGoggles.NightVisionData DefaultnightVisionState = new NightVisionGoggles.NightVisionData(b:true);
		private bool DefaultBlind = false;
		private uint DefaultBadEyesight = 0;
		public ColourBlindMode DefaultColourblindness = ColourBlindMode.None;
		private TRayScanner.Mode TRayDefaultcurrentMode = TRayScanner.Mode.Off;
		private bool DefaultHasEyecorrection = false;
		public MultiInterestBool XRay = new MultiInterestBool(); //This example if you have scenario where multiple things are interested in editing an ability

		#endregion
		#region SyncVars

		//HiddenHands
		[SyncVar(hook = nameof(SyncHiddenHands))]
		private HiddenHandValue hiddenHandSelection;

		[SyncVar(hook = nameof(SyncNightVision))]
		private NightVisionGoggles.NightVisionData nightVisionState = new NightVisionGoggles.NightVisionData(b:true);

		[SyncVar(hook = nameof(SyncBlindness))]
		public bool isBlind = false; //TODO change to multi-interest bool, Is good enough for now, For multiple eyes

		[SyncVar(hook = nameof(SyncBadEyesight))]
		public uint BadEyesight = 0;

		[SyncVar(hook = nameof(SyncColourBlindMode))]
		public ColourBlindMode CurrentColourblindness = ColourBlindMode.None;

		[SyncVar(hook = nameof(SyncXrayState))]
		public bool SyncXRay = false;

		[SyncVar(hook = nameof(TRaySyncMode))]
		private TRayScanner.Mode TRayCurrentMode = TRayScanner.Mode.Off;
		//Antag
		[SyncVar(hook = nameof(SyncAntagState))]
		private bool isAntag;
		public bool IsAntag => isAntag;

		[SyncVar(hook = nameof(SyncEyecorrection))]
		public bool HasEyecorrection = false;


		#endregion






		#region OtherVariables

		public bool ClientForThisBody => OverrideLocalPlayer || isLocalPlayer;

		public bool OverrideLocalPlayer = false;

		//NightVision
		private CameraEffectControlScript cameraEffectControlScript;

		private LightingSystem lightingSystem;

		#endregion

		#region LifeCycle

		public void ClientOnPlayerLeaveBody()
		{
			ApplyValues(true);
		}

		public void ClientOnPlayerTransferProcess()
		{
			ApplyValues(false);
		}

		public void ApplyValues(bool Leaving) //TODO Add your custom synchronised value here
		{
			OverrideLocalPlayer = true;
			//SyncXrayState(No need to apply values, if (Leaving) ?  = Reset the default InitialXRay : Otherwise apply current state XRay);
			SyncXrayState(true, Leaving ? XRay.initialState : XRay);
			TRaySyncMode(TRayScanner.Mode.Wires, Leaving ? TRayDefaultcurrentMode : TRayCurrentMode);
			var nn = new NightVisionGoggles.NightVisionData(b: true) { isOn = true};
			SyncNightVision(nn, Leaving ? DefaultnightVisionState : nightVisionState);
			SyncBlindness(true, Leaving ? DefaultBlind : isBlind );
			SyncBadEyesight(1, Leaving ? DefaultBadEyesight : BadEyesight);
			SyncColourBlindMode(ColourBlindMode.Deuntan, Leaving ? DefaultColourblindness : CurrentColourblindness);
			SyncEyecorrection(true,Leaving ? DefaultHasEyecorrection : HasEyecorrection );
			OverrideLocalPlayer = false;
		}

		private void Awake()
		{
			cameraEffectControlScript = Camera.main.OrNull()?.GetComponent<CameraEffectControlScript>();
			lightingSystem = Camera.main.GetComponent<LightingSystem>();
			XRay.OnBoolChange.AddListener((bool Value) => { SyncXrayState(false, Value);  } );
		}

		#endregion

		#region Server

		[Server]
		public void ServerSetHiddenHands(HiddenHandValue newState)
        {
			hiddenHandSelection = newState;
        }


		[Server]
		public void ServerSetAntag(bool newState)
		{
			isAntag = newState;
			GetComponent<PlayerScript>().ActivateAntagAction(newState);
		}

		#endregion

		#region Client

		[Client]
		private void SyncHiddenHands(HiddenHandValue oldState, HiddenHandValue newState)
        {
			hiddenHandSelection = newState;
			HandsController.Instance.HideHands(hiddenHandSelection);
        }

		public void SyncNightVision(NightVisionGoggles.NightVisionData oldState, NightVisionGoggles.NightVisionData newState)
		{
			// fov Occlusion spread = 3 0 if off
			if ((oldState.isOn == false && isServer))
			{
				nightVisionState = newState;
			}


			if (ClientForThisBody)
			{
				cameraEffectControlScript.AdjustPlayerVisibility(newState.nightVisionVisibility, newState.isOn ? newState.visibilityAnimationSpeed : 0.1f);
				cameraEffectControlScript.ToggleNightVisionEffectState(newState.isOn);
			}
		}

		[Client]
		private void SyncAntagState(bool oldState, bool newState)
		{
			isAntag = newState;
		}

		public void SyncBlindness(bool NotSetValueServer, bool newState)
		{
			//0.65 = 15 By default
			if ((NotSetValueServer == false && isServer))
			{
				isBlind = newState;
			}

			if (ClientForThisBody)
			{
				if (newState)
				{
					lightingSystem.fovDistance = 0.65f;
				}
				else
				{
					lightingSystem.fovDistance = 15;
				}
			}
		}

		public void SyncBadEyesight(uint NotSetValueServer, uint newState)
		{
			if ((NotSetValueServer == 0 && isServer))
			{
				BadEyesight = newState;
			}

			if (ClientForThisBody)
			{
				SetBlurryVisionState(HasEyecorrection, newState);

			}
		}


		private void SyncXrayState(bool NotSetValueServer, bool newState)
		{
			// fov Occlusion spread = 3 0 if off
			if ((NotSetValueServer == false && isServer))
			{
				SyncXRay = newState;
			}

			if (ClientForThisBody)
			{
				if (newState)
				{
					lightingSystem.renderSettings.fovOcclusionSpread = 3;
				}
				else
				{
					lightingSystem.renderSettings.fovOcclusionSpread = 0;
				}
			}
		}

		public void SyncColourBlindMode(ColourBlindMode NotSetValueServer, ColourBlindMode newState)
		{

			if ((NotSetValueServer == ColourBlindMode.None && isServer))
			{
				CurrentColourblindness = newState;
			}

			if (ClientForThisBody)
			{
				cameraEffectControlScript.colourblindEmulationEffect.SetColourMode(newState);
			}
		}


		public void SyncEyecorrection(bool NotSetValueServer, bool newState)
		{
			// fov Occlusion spread = 3 0 if off
			if ((NotSetValueServer == false && isServer))
			{
				HasEyecorrection = newState;
			}

			if (ClientForThisBody)
			{
				SetBlurryVisionState(newState, BadEyesight);
			}
		}

		public void TRaySyncMode(TRayScanner.Mode oldMode, TRayScanner.Mode newMode)
		{
			if ((oldMode == TRayScanner.Mode.Off && isServer))
			{
				TRayCurrentMode = newMode;
			}

			if (ClientForThisBody)
			{
				var matrixInfos = MatrixManager.Instance.ActiveMatricesList;

				foreach (var matrixInfo in matrixInfos)
				{
					var electricalRenderer = matrixInfo.Matrix.ElectricalLayer.GetComponent<TilemapRenderer>();
					var pipeRenderer = matrixInfo.Matrix.PipeLayer.GetComponent<TilemapRenderer>();
					var disposalsRenderer = matrixInfo.Matrix.DisposalsLayer.GetComponent<TilemapRenderer>();

					//Turn them all off
					ChangeState(electricalRenderer, false, 2);
					ChangeState(pipeRenderer, false, 1);
					ChangeState(disposalsRenderer, false);

					switch (newMode)
					{
						case TRayScanner.Mode.Off:
							continue;
						case TRayScanner.Mode.Wires:
							ChangeState(electricalRenderer, true);
							continue;
						case TRayScanner.Mode.Pipes:
							ChangeState(pipeRenderer, true);
							continue;
						case TRayScanner.Mode.Disposals:
							ChangeState(disposalsRenderer, true);
							continue;
						default:
							Logger.LogError($"Found no case for {newMode}");
							continue;
					}
				}
			}
		}


		private void ChangeState(TilemapRenderer tileRenderer, bool state, int oldLayerOrder = 0)
		{
			tileRenderer.sortingLayerName = state ? "Walls" : "UnderFloor";
			tileRenderer.sortingOrder = state ? 100 : oldLayerOrder;
		}

		#endregion


		private void SetBlurryVisionState(bool InHasEyeCorrection, uint BlurryStrength)
		{
			if (ClientForThisBody)
			{
				if (InHasEyeCorrection)
				{
					cameraEffectControlScript.blurryVisionEffect.SetBlurStrength((int)BlurryStrength);
				}
				else
				{
					cameraEffectControlScript.blurryVisionEffect.SetBlurStrength(0);
				}
			}
		}
	}
}
