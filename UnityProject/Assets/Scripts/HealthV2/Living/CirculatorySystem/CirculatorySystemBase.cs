using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using Chemistry.Components;
using HealthV2.Living.CirculatorySystem;
using Items.Implants.Organs;
using NaughtyAttributes;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	public class CirculatorySystemBase : MonoBehaviour
	{

	}


	public enum BleedingState
	{
		None,
		VeryLow,
		Low,
		Medium,
		High,
		UhOh
	}
}
