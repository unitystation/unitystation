using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "Respiratory Info", menuName = "ScriptableObjects/Health/RespiratoryInfo", order = 0)]
public class RespiratoryInfo : ScriptableObject
{
	//Not sure if this belongs here. Seems like something the lungs organ should handle.
	[Required("A gas must be used to respire.")]
	public Gas RequiredGas = Gas.Oxygen;

	//Not actually sure if this belongs here. Seems like something the lungs organ should handle.
	[Tooltip("The gas this respiratory system will release.")]
	public Gas ReleasedGas = Gas.CarbonDioxide;

	[Tooltip("The cooldown between breaths in ticks.")]
	[Required("Must supply a cooldown for breathing. It is measured in ticks.")]
	public int breathCooldown = 4;

	//The minimum pressure the required gas can be at before we start suffering damage.
	public float MinimumSafePressure = 16;
}
