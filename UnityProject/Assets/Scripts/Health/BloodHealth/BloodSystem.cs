using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the blood system for a Living Entity
/// Only updated and monitored on the Server!
/// Do not derive this class from NetworkBehaviour
/// </summary>
public class BloodSystem : MonoBehaviour
{
	public int ToxinDamage { get; set; } = 0;
	public int OxygenLevel { get; set; } = 100; //100% is full healthy levels of oxygen
	private LivingHealthBehaviour livingHealthBehaviour;
	private DNAandBloodType bloodType;
	private readonly float bleedRate = 2f;
	private int bleedVolume;
	public int BloodLevel = (int)BloodVolume.NORMAL;
	public bool IsBleeding { get; private set; }

	void Awake()
	{
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
	}

	//Initial setting for blood type. Server only
	public void SetBloodType(DNAandBloodType dnaBloodType)
	{
		bloodType = dnaBloodType;
	}
}