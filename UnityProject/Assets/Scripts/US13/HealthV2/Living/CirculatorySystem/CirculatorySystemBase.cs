using UnityEngine;

namespace US13.HealthV2.Living.CirculatorySystem
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
