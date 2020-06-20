using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GUI_ReactorControler : NetTab
{
	public ReactorControlConsole ReactorControlConsole;

	public GUI_ReactorLayout GUIReactorLayout = new GUI_ReactorLayout();
	public GUI_ReactorAnnunciator GUIReactorAnnunciator = new GUI_ReactorAnnunciator();
	[SerializeField] private NetSliderDial ReactorCoreTemperature =null;
	[SerializeField] private NetSliderDial CorePressure =null;
	[SerializeField] private NetSliderDial CoreWaterLevel =null;
	[SerializeField] private NetSliderDial CoreKValue =null;
	[SerializeField] private NetSliderDial CoreFluxLevel =null;
	private decimal PreviousRADlevel = 0;
	public decimal PercentageChange = 0;

	void Start()
	{
		if (Provider != null)
		{
			ReactorControlConsole = Provider.GetComponentInChildren<ReactorControlConsole>();
		}

		GUIReactorLayout.GUI_ReactorControler = this;
		GUIReactorAnnunciator.GUIReactorControler = this;
		GUIReactorLayout.Start();
	}

	private void OnEnable()
	{
		base.OnEnable();
		UpdateManager.Add(Refresh, 1);
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}


	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Refresh);
	}

	public void Refresh()
	{
		//
		//optimise
		//Change to dials and things
		if (ReactorControlConsole != null)
		{
			var tep = ReactorControlConsole.ReactorChambers.Temperature;
			ReactorCoreTemperature.SetValueServer(Math.Round((tep / 1200) * 100).ToString());

			CorePressure.SetValueServer(Math
				.Round((ReactorControlConsole.ReactorChambers.CurrentPressure /
				        ReactorControlConsole.ReactorChambers.MaxPressure) * 100).ToString());

			CoreWaterLevel.SetValueServer((Math.Round((ReactorControlConsole.ReactorChambers.ReactorPipe.pipeData
				.mixAndVolume.Mix
				.Total / 240) * 100)).ToString());


			if (PreviousRADlevel != 0 && ReactorControlConsole.ReactorChambers.PresentNeutrons != 0)
			{
				PercentageChange =
					((ReactorControlConsole.ReactorChambers.PresentNeutrons / PreviousRADlevel) * 100);

				PercentageChange = PercentageChange - 100;
				var ValuePercentageChange = 50 + (PercentageChange * 5);
				if (ValuePercentageChange < 0)
				{
					ValuePercentageChange = 0;
				}
				else if (ValuePercentageChange > 100)
				{
					ValuePercentageChange = 100;
				}

				CoreKValue.SetValueServer(Math.Round(ValuePercentageChange).ToString());
			}


			PreviousRADlevel = ReactorControlConsole.ReactorChambers.PresentNeutrons;

			decimal Value = 0;

			if (PreviousRADlevel > 1000000000000)
			{
				Value = (100m / 13m) * 12 + ((100m / 13m) * PreviousRADlevel / 10000000000000);
			}
			else if (PreviousRADlevel > 100000000000)
			{
				Value = (100m / 13m) * 11 + ((100m / 13m) * PreviousRADlevel / 1000000000000);
			}
			else if (PreviousRADlevel > 10000000000)
			{
				Value = (100m / 13m) * 10 + ((100m / 13m) * PreviousRADlevel / 100000000000);
			}
			else if (PreviousRADlevel > 1000000000)
			{
				Value = (100m / 13m) * 9 + ((100m / 13m) * PreviousRADlevel / 10000000000);
			}
			else if (PreviousRADlevel > 100000000)
			{
				Value = (100m / 13m) * 8 + ((100m / 13m) * PreviousRADlevel / 1000000000);
			}
			else if (PreviousRADlevel > 10000000)
			{
				Value = (100m / 13m) * 7 + ((100m / 13m) * PreviousRADlevel / 100000000);
			}
			else if (PreviousRADlevel > 1000000)
			{
				Value = (100m / 13m) * 6 + ((100m / 13m) * PreviousRADlevel / 10000000);
			}
			else if (PreviousRADlevel > 100000)
			{
				Value = (100m / 13m) * 5 + ((100m / 13m) * PreviousRADlevel / 1000000);
			}
			else if (PreviousRADlevel > 10000)
			{
				Value = (100m / 13m) * 4 + ((100m / 13m) * PreviousRADlevel / 100000);
			}
			else if (PreviousRADlevel > 1000)
			{
				Value = (100m / 13m) * 3 + ((100m / 13m) * PreviousRADlevel / 10000);
			}
			else if (PreviousRADlevel > 100)
			{
				Value = (100m / 13m) * 2 + ((100m / 13m) * PreviousRADlevel / 1000);
			}
			else if (PreviousRADlevel > 10)
			{
				Value = (100m / 13m) * 1 + ((100m / 13m) * PreviousRADlevel / 100);
			}
			else if (PreviousRADlevel > 0)
			{
				Value = ((100m / 13m) * PreviousRADlevel / 10);
			}


			CoreFluxLevel.SetValueServer((Math.Round(Value).ToString()));
			GUIReactorLayout.Refresh();
			GUIReactorAnnunciator.Refresh();
		}
	}

	/// <summary> As
	///  Set the control rod Depth percentage
	/// </summary>
	/// <param name="speedMultiplier"></param>
	public void SetControlDepth(float Depth)
	{
		//Logger.Log("YO " + Depth);
		ReactorControlConsole.SuchControllRodDepth(Depth);
	}

	[System.Serializable]
	public class GUI_ReactorLayout
	{
		public Color RodFuel = Color.red;
		public Color RodControl = Color.green;
		public GUI_ReactorControler GUI_ReactorControler;

		public List<NetColorChanger> RodChamber = new List<NetColorChanger>();

		public void Start()
		{
			for (int i = 0; i < 16; i++)
			{
				RodChamber.Add(GUI_ReactorControler["ReactorSlot (" + i + ")"] as NetColorChanger);
			}
		}

		public void Refresh()
		{
			for (int i = 0; i < GUI_ReactorControler.ReactorControlConsole.ReactorChambers.ReactorRods.Length; i++)
			{
				if (GUI_ReactorControler.ReactorControlConsole.ReactorChambers.ReactorRods[i] != null)
				{
					var TheType = GUI_ReactorControler.ReactorControlConsole.ReactorChambers.ReactorRods[i]
						.GetUIColour();


					RodChamber[i].SetValueServer(TheType);
				}
				else
				{
					RodChamber[i].SetValueServer(Color.gray);
				}
			}
		}
	}

	[System.Serializable]
	public class GUI_ReactorAnnunciator
	{
		public GUI_ReactorControler GUIReactorControler = null;

		public NetFlasher HighTemperature;
		public NetFlasher HighTemperatureDelta;
		public NetFlasher LowTemperature;
		public NetFlasher HighNeutronFlux;
		public NetFlasher HighNeutronFluxDelta;
		public NetFlasher LowNeutronFlux;
		public NetFlasher PositiveKValue;
		public NetFlasher LowKValue;
		public NetFlasher LowWaterLevel;

		public NetFlasher HighWaterLevel;
		public NetFlasher HighCorePressure;
		public NetFlasher HighPressureDelta;
		public NetFlasher LowControlRodDepth;
		public NetFlasher HighControlRodDepth;

		public NetFlasher CoreMeltdown;

		public NetFlasher Clown;
		public NetFlasher HighDegradationOfFuel;
		public NetFlasher CorePipeBurst;

		public void Refresh()
		{
			Temperature();
			NeutronFlux();
			KValues();
			WaterLevel();
			Pressure();
			RobDepth();

			var Chamber = GUIReactorControler.ReactorControlConsole.ReactorChambers;
			CoreMeltdown.SetState(Chamber.MeltedDown);
			HighDegradationOfFuel.SetState(
				Chamber.ReactorFuelRods.Any(x => (x.PresentAtomsfuel / x.PresentAtoms) < 0.65m));

			CorePipeBurst.SetState(
				Chamber.PoppedPipes);
		}

		public float Last_Temperature = 200;
		public float Temperature_Delta = 0;

		public void Temperature()
		{
			var Chamber = GUIReactorControler.ReactorControlConsole.ReactorChambers;

			HighTemperature.SetState(
				(Chamber.RodMeltingTemperatureK - 200) < Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Temperature);

			Temperature_Delta = Math.Abs(Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Temperature - Last_Temperature);

			HighTemperatureDelta.SetState(Temperature_Delta > 10);

			LowTemperature.SetState(Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Temperature < 373.15f);

			Last_Temperature = Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Temperature;
		}


		public decimal Last_HighNeutronFluxDelta = 0;
		public decimal HighNeutronFluxDelta_Delta = 0;

		public void NeutronFlux()
		{
			var Chamber = GUIReactorControler.ReactorControlConsole.ReactorChambers;
			HighNeutronFlux.SetState(Chamber.PresentNeutrons > (Chamber.NeutronSingularity - 1000000));
			Last_HighNeutronFluxDelta = Math.Abs(Chamber.PresentNeutrons - Last_HighNeutronFluxDelta);
			HighNeutronFluxDelta.SetState(HighNeutronFluxDelta_Delta > 100000);
			LowNeutronFlux.SetState(Chamber.PresentNeutrons < 200);
			Last_HighNeutronFluxDelta = Chamber.PresentNeutrons;

		}

		public void KValues()
		{
			PositiveKValue.SetState(GUIReactorControler.PercentageChange > 2);
			LowKValue.SetState(GUIReactorControler.PercentageChange < -2);
		}


		public void WaterLevel()
		{
			var Chamber = GUIReactorControler.ReactorControlConsole.ReactorChambers;
			LowWaterLevel.SetState(Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Total < 25);
			HighWaterLevel.SetState(Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Total > 190);
		}

		public float Last_Pressure = 0;
		public float Pressure_Delta = 0;

		public void Pressure()
		{
			var Chamber = GUIReactorControler.ReactorControlConsole.ReactorChambers;
			float Pressure = Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Temperature *
			                 Chamber.ReactorPipe.pipeData.mixAndVolume.Mix.Total;

			HighCorePressure.SetState(Pressure > (float) (Chamber.MaxPressure - 10000));

			Pressure_Delta = Math.Abs(Pressure - Last_Pressure);
			HighPressureDelta.SetState(Pressure_Delta > 100);
			Last_Pressure = Pressure;
		}

		public void RobDepth()
		{
			var Chamber = GUIReactorControler.ReactorControlConsole.ReactorChambers;
			LowControlRodDepth.SetState(Chamber.ControlRodDepthPercentage < 0.1f);
			HighControlRodDepth.SetState(Chamber.ControlRodDepthPercentage > 0.80f);
		}
	}
}