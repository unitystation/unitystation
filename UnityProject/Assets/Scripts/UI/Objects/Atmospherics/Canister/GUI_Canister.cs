using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AdminTools;
using UI.Core;
using Objects.Atmospherics;


namespace UI.Objects.Atmospherics
{
	public class GUI_Canister : NetTab
	{
		private static readonly float PRESSURE_UPDATE_RATE = 0.5f;

		private Canister canister;

		public Image BG;
		public Image InnerPanelBG;
		public Text LabelText;
		public NumberSpinner InternalPressureDial;
		public NumberSpinner ExternalPressureDial;
		public NumberSpinner ReleasePressureDial;
		public NetWheel ReleasePressureWheel;

		public GameObject EditReleasePressurePopup;
		public Image XButton;
		public NetLabel ConnectionStatus;
		//release lever to open canister internals
		public NetToggle PrimaryReleaseLever;
		//release lever to switch between the main valve an the external tank
		public NetToggle SecondaryReleaseLever;

		//external tank
		public NetLabel externalTankStatus;
		public NetSpriteImage externalTankImage;

		//LED stuff
		public Graphic Red;
		public Graphic Green;
		public Graphic Yellow;
		private static readonly Color RED_ACTIVE = DebugTools.HexToColor("FF1C00");
		private static readonly Color RED_INACTIVE = DebugTools.HexToColor("730000");
		private static readonly Color YELLOW_ACTIVE = DebugTools.HexToColor("E4FF02");
		private static readonly Color YELLOW_INACTIVE = DebugTools.HexToColor("5E5400");
		private static readonly Color GREEN_ACTIVE = DebugTools.HexToColor("02FF23");
		private static readonly Color GREEN_INACTIVE = DebugTools.HexToColor("005E00");
		private bool flashingRed;
		private float secondsSinceFlash;
		private static readonly float SECONDS_PER_FLASH = 0.3f;

		private static readonly float GreenLowerBound = 10 * AtmosConstants.ONE_ATMOSPHERE;
		private static readonly float YellowLowerBound = 5 * AtmosConstants.ONE_ATMOSPHERE;
		private static readonly float RedLowerBound = 10f;

		//for fade in / out of hiss
		private static readonly float HISS_LERP_PER_SECOND = 0.1f;
		private static readonly float HISS_MAX_VOLUME = 0.3f;
		private static readonly float HISS_MIN_VOLUME = 0.125f;
		//maximum rate of change of internal pressure to achieve max hiss volume.
		private static readonly float HISS_MAX_RATE = 500;
		private AudioSource hiss;
		//used to lerp from current to target volume
		private float targetHissVolume;
		private float currentHissVolume;
		//how much time has elapsed since pressure has changed - we stop hissing once we
		//have not recieved a pressure change in awhile
		private float timeSincePressureChange;
		private float prevInternalPressure;
		private bool muteSounds = false;
		/// <summary>
		/// Whether sounds should be muted on this instance of the UI.
		/// </summary>
		public bool MuteSounds => muteSounds;
		private GasContainer gasContainer => canister.GasContainer;

		private enum ExternalTank
		{
			NoTank = 0,
			TankInserted = 1
		}

		#region Lifecycle

		private void Awake()
		{
			muteSounds = IsServer;
			hiss = GetComponent<AudioSource>();
		}

		protected override void InitServer()
		{
			StartCoroutine(ServerWaitForProvider());
		}

		private IEnumerator WaitToEnableInput()
		{
			yield return WaitFor.EndOfFrame;
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		//server side initialization
		private IEnumerator ServerWaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			canister = Provider.GetComponent<Canister>();

			//init pressure dials
			InternalPressureDial.ServerSpinTo(Mathf.RoundToInt(gasContainer.ServerInternalPressure));
			ReleasePressureDial.ServerSpinTo(Mathf.RoundToInt(gasContainer.ReleasePressure));
			if (canister.HasContainerInserted)
			{
				GasContainer externalTank = canister.InsertedContainer.GetComponent<GasContainer>();
				ExternalPressureDial.ServerSpinTo(Mathf.RoundToInt(externalTank.ServerInternalPressure));
			}
			else
			{
				ExternalPressureDial.ServerSpinTo(0);
			}
			//init connection status
			canister.ServerOnConnectionStatusChange.AddListener(ServerUpdateConnectionStatus);
			ServerUpdateConnectionStatus(canister.IsConnected);
			//init external tank status
			canister.ServerOnExternalTankInserted.AddListener(ServerUpdateExternalTank);
			ServerUpdateExternalTank(canister.HasContainerInserted);

			//init wheel
			ReleasePressureWheel.SetValueServer(Mathf.RoundToInt(gasContainer.ReleasePressure).ToString());
			StartCoroutine(ServerRefreshInternalPressure());
		}

		//client side  initialization
		private IEnumerator ClientWaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			//set the tab color and label based on the provider
			canister = Provider.GetComponent<Canister>();
			BG.color = canister.UIBGTint;
			InnerPanelBG.color = canister.UIInnerPanelTint;
			LabelText.text = "Contains " + canister.ContentsName;
			XButton.color = canister.UIBGTint;

			OnInternalPressureChanged(InternalPressureDial.SyncedValue);
			InternalPressureDial.OnSyncedValueChanged.AddListener(OnInternalPressureChanged);
		}

		public override void OnEnable()
		{
			base.OnEnable();
			StartCoroutine(ClientWaitForProvider());
		}

		private void OnDisable()
		{
			hiss.Stop();
		}

		#endregion Lifecycle

		/// <summary>
		/// Updates the displayed external tank
		/// </summary>
		private void ServerUpdateExternalTank(bool externalExists)
		{
			GameObject insertedContainer = canister.InsertedContainer;

			if (externalExists)
			{
				externalTankStatus.SetValueServer(insertedContainer.Item().InitialName);
				externalTankImage.SetSprite((int) ExternalTank.TankInserted);
				GasContainer externalTank = insertedContainer.GetComponent<GasContainer>();
				ExternalPressureDial.ServerSpinTo(Mathf.RoundToInt(externalTank.ServerInternalPressure));

			}
			else
			{
				externalTankStatus.SetValueServer("No Tank Inserted");
				externalTankImage.SetSprite((int) ExternalTank.NoTank);
				ExternalPressureDial.ServerSpinTo(0);
			}
		}

		/// <summary>
		/// Updates the displayed connection status.
		/// </summary>
		private void ServerUpdateConnectionStatus(bool isConnected)
		{
			ConnectionStatus.SetValueServer(isConnected ? "Connected" : "Not Connected");
		}

		/// <summary>
		/// Updates the LEDs at the top to display the correct color based on the
		/// specified pressure.
		/// </summary>
		/// <param name="pressure"></param>
		private void OnInternalPressureChanged(int pressure)
		{
			//update LEDs
			if (pressure > GreenLowerBound)
			{
				flashingRed = false;
				Red.color = RED_INACTIVE;
				Yellow.color = YELLOW_INACTIVE;
				Green.color = GREEN_ACTIVE;
			}
			else if (pressure > YellowLowerBound)
			{
				flashingRed = false;
				Red.color = RED_INACTIVE;
				Yellow.color = YELLOW_ACTIVE;
				Green.color = GREEN_INACTIVE;
			}
			else if (pressure > RedLowerBound)
			{
				//flashing red (if not already)
				if (!flashingRed)
				{
					flashingRed = true;
					Red.color = RED_ACTIVE;
					Yellow.color = YELLOW_INACTIVE;
					Green.color = GREEN_INACTIVE;
				}
			}
			else
			{
				//empty
				flashingRed = false;
				Red.color = RED_INACTIVE;
				Yellow.color = YELLOW_INACTIVE;
				Green.color = GREEN_INACTIVE;
			}

			//hissing
			if (!muteSounds)
			{
				var rate = prevInternalPressure - pressure;
				prevInternalPressure = pressure;
				if (PrimaryReleaseLever.Element.isOn && rate > 0)
				{
					//we lost pressure, hiss
					timeSincePressureChange = 0f;
					//if not hissing, start
					if (!hiss.isPlaying)
					{
						hiss.volume = 0;
						hiss.Play();
					}

					//set target volume based on rate
					targetHissVolume = Mathf.Lerp(HISS_MIN_VOLUME, HISS_MAX_VOLUME, rate / HISS_MAX_RATE);
				}
			}
		}

		private void Update()
		{
			//if the red LED is lit up, it needs to flash.
			//This toggles the red LED on / off based on the elapsed time
			//since the last flash
			if (flashingRed)
			{
				secondsSinceFlash += Time.deltaTime;
				if (secondsSinceFlash >= SECONDS_PER_FLASH)
				{
					secondsSinceFlash = 0;
					var curColor = Red.color;
					if (curColor == RED_ACTIVE)
					{
						Red.color = RED_INACTIVE;
					}
					else
					{
						Red.color = RED_ACTIVE;
					}
				}
			}

			//hissing update
			if (!muteSounds)
			{
				if (hiss.isPlaying)
				{
					timeSincePressureChange += Time.deltaTime;
					//currently hissing
					//stop playing sound if pressure hasn't changed in awhile
					//or if release lever is closed
					if (timeSincePressureChange > PRESSURE_UPDATE_RATE * 1.5)
					{
						targetHissVolume = 0;
					}
					//lerp hiss volume
					if (targetHissVolume != currentHissVolume)
					{
						currentHissVolume = Mathf.MoveTowards(currentHissVolume, targetHissVolume, Time.deltaTime * HISS_LERP_PER_SECOND);
						hiss.volume = currentHissVolume;
					}
					//stop playing when we reach 0
					//or release lever is closed
					if (currentHissVolume == 0 || !PrimaryReleaseLever.Element.isOn)
					{
						//will restart from 0 volume when resuming
						currentHissVolume = 0;
						hiss.Stop();
					}
				}
			}


		}

		public void OpenPopup()
		{
			EditReleasePressurePopup.SetActive(true);
			EditReleasePressurePopup.GetComponentInChildren<InputFieldFocus>().Select();
		}

		public void ClosePopup()
		{
			EditReleasePressurePopup.SetActive(false);
			StartCoroutine(WaitToEnableInput());
		}

		private IEnumerator ServerRefreshInternalPressure()
		{
			var currentValue = Mathf.RoundToInt(gasContainer.ServerInternalPressure);
			//only update if it changed
			if (InternalPressureDial.SyncedValue != currentValue)
			{
				InternalPressureDial.ServerSpinTo(currentValue);
				if (canister.InsertedContainer != null && canister.tankValveOpen)
				{
					GasContainer externalTank = canister.InsertedContainer.GetComponent<GasContainer>();
					ExternalPressureDial.ServerSpinTo(Mathf.RoundToInt(externalTank.ServerInternalPressure));
				}
			}

			yield return WaitFor.Seconds(PRESSURE_UPDATE_RATE);
			StartCoroutine(ServerRefreshInternalPressure());
		}

		/// <summary>
		/// Update the actual release pressure and all the attached UI elements
		/// </summary>
		/// <param name="newValue"></param>
		public void ServerUpdateReleasePressure(float newValue)
		{
			gasContainer.ReleasePressure = newValue;
			int intValue = Mathf.RoundToInt(newValue);
			ReleasePressureDial.ServerSpinTo(intValue);
			ReleasePressureWheel.SetValueServer(intValue.ToString()) ;
		}

		/// <summary>
		/// Allows for adding / subtracting from release pressure
		/// </summary>
		/// <param name="offset"></param>
		public void ServerAdjustReleasePressure(int offset)
		{
			ServerUpdateReleasePressure(Mathf.RoundToInt(gasContainer.ReleasePressure + offset));
		}

		/// <summary>
		/// So we can edit using the free text entry
		/// </summary>
		/// <param name="newValue"></param>
		public void ServerEditReleasePressure(string newValue)
		{
			if (string.IsNullOrEmpty(newValue)) return;
			if (float.TryParse(newValue, out var releasePressure))
			{
				releasePressure = Mathf.Clamp(releasePressure, 0, Canister.MAX_RELEASE_PRESSURE);
				ServerUpdateReleasePressure(releasePressure);
			}
		}

		/// <summary>
		/// Open / close the release valve of the attached container
		/// </summary>
		/// <param name="isOpen"></param>
		public void ServerToggleRelease(bool isOpen)
		{
			VentContainer(isOpen);
		}

		/// <summary>
		/// switch the secondary valve between internals valve and external tank
		/// </summary>
		/// <param name="usingTank">Is the valve set to tank.</param>
		public void ServerToggleSecondary(bool usingTank)
		{
			canister.tankValveOpen = usingTank;

			if (usingTank)
			{
				if (canister.HasContainerInserted)
				{
					canister.MergeCanisterAndTank();
					GasContainer externalTank = canister.InsertedContainer.GetComponent<GasContainer>();
					ExternalPressureDial.ServerSpinTo(Mathf.RoundToInt(externalTank.ServerInternalPressure));
				}
				else
				{
					StartCoroutine(DisplayFlashingText("Insert a tank before opening the valve!", 1F));
				}
			}
		}

		public void EjectExternalTank()
		{
			if (canister.HasContainerInserted)
			{
				if (canister.tankValveOpen)
				{
					StartCoroutine(DisplayFlashingText("Close the valve first!"));
				}
				else
				{
					canister.RetrieveInsertedContainer();
					StartCoroutine(DisplayFlashingText("Tank ejected!"));
				}
			}
			else
			{
				StartCoroutine(DisplayFlashingText("No Tank Inserted"));
			}
		}

		private void VentContainer(bool isOpen)
		{
			canister.SetValve(isOpen);

			if (canister.GasContainer.IsVenting)
			{
				StartCoroutine(DisplayFlashingText($"Canister releasing at {gasContainer.ReleasePressure}"));
				if (canister.ContentsName.Contains("Plasma"))
				{
					foreach (var p in Peepers)
					{
						AutoMod.ProcessPlasmaRelease(p);
					}
				}
			}
		}

		private IEnumerator DisplayFlashingText(string text, float speed = 0.5F)
		{
			string initialInfoText = externalTankStatus.Element.text;
			externalTankStatus.SetValueServer(text);
			yield return WaitFor.Seconds(speed);
			externalTankStatus.SetValueServer("");
			yield return WaitFor.Seconds(speed / 2);
			externalTankStatus.SetValueServer(text);
			yield return WaitFor.Seconds(speed);
			externalTankStatus.SetValueServer("");
			yield return WaitFor.Seconds(speed / 2);
			externalTankStatus.SetValueServer(text);
			yield return WaitFor.Seconds(speed);
			externalTankStatus.SetValueServer("");
			yield return WaitFor.Seconds(speed / 2);

			if (canister.InsertedContainer != null)
			{
				externalTankStatus.SetValueServer($"{canister.InsertedContainer.Item().InitialName}");
			}
			else
			{
				externalTankStatus.SetValueServer("No Tank Inserted");
			}

		}
	}
}
