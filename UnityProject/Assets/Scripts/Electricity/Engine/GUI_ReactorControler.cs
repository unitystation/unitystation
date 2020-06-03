using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ReactorControler : NetTab
{
	public ReactorControlConsole ReactorControlConsole;

	public  GUI_ReactorLayout GUIReactorLayout = new GUI_ReactorLayout();

	[SerializeField] private NetLabel ReactorCoreTemperatureText;
	[SerializeField] private NetLabel RadiationLevelAboveCore;
	[SerializeField] private NetLabel CorePressure;
	[SerializeField] private NetLabel CoreControlRodDepth;
	[SerializeField] private NetLabel CoreWaterLevel;
	[SerializeField] private NetLabel CoreKValue;
	[SerializeField] private NetLabel CoreFluxLevel;
	private decimal PreviousRADlevel = 0;
	private decimal PercentageChange = 0;
	void Start()
	{
		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			ReactorControlConsole = Provider.GetComponentInChildren<ReactorControlConsole>();
		}

		GUIReactorLayout.GUI_ReactorControler = this;
		GUIReactorLayout.Start();
	}

	private void OnEnable()
	{
		base.OnEnable();
		UpdateManager.Add(Refresh, 1);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Refresh);
	}

	public void Refresh()
	{
		//optimise
		//Change to dials and things
		ReactorCoreTemperatureText.SetValueServer("core tem " + ReactorControlConsole.ReactorChambers[0].Temperature + " K ");
		RadiationLevelAboveCore.SetValueServer("Rad Above core " + ReactorControlConsole.ReactorChambers[0].RadiationAboveCore() + "RAD");
		CorePressure.SetValueServer("CorePressure " + ReactorControlConsole.ReactorChambers[0].CurrentPressureInput );
		CoreControlRodDepth.SetValueServer("Core Control Rod Depth " + ReactorControlConsole.ReactorChambers[0].ControlRodDepthPercentage *100 + "% ");
		CoreWaterLevel.SetValueServer("Core Fluid level " + ReactorControlConsole.ReactorChambers[0].ReactorPipe.pipeData.mixAndVolume.Mix.Total  + "U ");
		if (PreviousRADlevel != 0 && ReactorControlConsole.ReactorChambers[0].PresentNeutrons != 0)
		{
			PercentageChange =
				((ReactorControlConsole.ReactorChambers[0].PresentNeutrons/PreviousRADlevel) * 100);
			if (PercentageChange < 100)
			{
				PercentageChange = PercentageChange - 100;
			}

		}
		PreviousRADlevel = ReactorControlConsole.ReactorChambers[0].PresentNeutrons;

		CoreKValue.SetValueServer("Core K Value " + Math.Round(PercentageChange, 3) + "% " );
		CoreFluxLevel.SetValueServer("Core Flux value " + Math.Round(PreviousRADlevel, 3) + " RADs" );
		GUIReactorLayout.Refresh();
	}

	/// <summary>
	/// Sets shuttle speed.
	/// </summary>
	/// <param name="speedMultiplier"></param>
	public void SetControlDepth(float Depth)
	{
		Logger.Log("YO " + Depth);
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
				RodChamber.Add( GUI_ReactorControler["ReactorSlot ("+i+")"] as NetColorChanger );
			}
		}

		public void Refresh()
		{
			for (int i = 0; i < GUI_ReactorControler.ReactorControlConsole.ReactorChambers[0].ReactorRods.Length; i++)
			{
				if (GUI_ReactorControler.ReactorControlConsole.ReactorChambers[0].ReactorRods[i] != null)
				{
					var TheType = GUI_ReactorControler.ReactorControlConsole.ReactorChambers[0].ReactorRods[i].GetRodType();
					if (TheType == RodType.Control)
					{
						RodChamber[i].SetValueServer(RodControl);
					}
					else
					{
						RodChamber[i].SetValueServer(RodFuel);
					}
				}
				else
				{
					RodChamber[i].SetValueServer(Color.gray);
				}

			}
		}

	}
}