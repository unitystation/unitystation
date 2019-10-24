using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic AI behaviour for being on guard and
/// attacking any targets that come into view
/// </summary>
[RequireComponent(typeof(ConeOfSight))]
[RequireComponent(typeof(MobAI))]
public class MobAttack : MobFollow
{
	//Add other targets at will
	public enum AttackTarget
	{
		players
	}

	private ConeOfSight coneOfSight;
	private MobAI mobAI;

	public override void OnEnable()
	{
		base.OnEnable();
		coneOfSight = GetComponent<ConeOfSight>();
	}
}
