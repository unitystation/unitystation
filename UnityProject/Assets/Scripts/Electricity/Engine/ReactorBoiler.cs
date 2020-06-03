using System;
using System.Collections;
using System.Collections.Generic;
using Pipes;
using UnityEngine;

public class ReactorBoiler : MonoBehaviour
{
	[SerializeField] private float tickRate = 1;
	private float tickCount;

	public decimal MaxPressureInput = 300000M;
	public decimal CurrentPressureInput = 0;
	public decimal OutputEnergy;
	public decimal TotalEnergyInput;

	public decimal Efficiency = 0.5M;
	public ReactorPipe ReactorPipe;
	//public ReactorTurbine reactorTurbine;
	public List<ReactorGraphiteChamber> Chambers;
	// Start is called before the first frame update

	private void OnEnable()
	{
		UpdateManager.Add(CycleUpdate, 1);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	public void Awake()
	{
		ReactorPipe = GetComponent<ReactorPipe>();
	}

	public void CycleUpdate()
	{
		//Maybe change equation later to something cool
		CurrentPressureInput = 0;
		CurrentPressureInput = (decimal) ((ReactorPipe.pipeData.mixAndVolume.Mix.Temperature - 293.15f) * ReactorPipe.pipeData.mixAndVolume.Mix.Temperature);
		if (CurrentPressureInput > 0)
		{
			ReactorPipe.pipeData.mixAndVolume.Mix.Temperature = ((((ReactorPipe.pipeData.mixAndVolume.Mix.Temperature  - 293.15f) *(float)  Efficiency) + 293.15f));
			//Logger.Log("CurrentPressureInput " + CurrentPressureInput);
			if (CurrentPressureInput > MaxPressureInput)
			{
				Logger.LogError(" ReactorBoiler !!!booommmm!!", Category.Editor);
			}
			OutputEnergy = CurrentPressureInput * Efficiency;
		}
		else
		{
			OutputEnergy = 0;
		}



	}
}