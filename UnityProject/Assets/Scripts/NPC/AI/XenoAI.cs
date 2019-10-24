using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy Xeno NPC's
/// Will attack any human that they see
/// </summary>
[RequireComponent(typeof(MobAttack))]
public class XenoAI : MobAI
{
	private MobAttack mobAttack;

	public override void OnEnable()
	{
		base.OnEnable();
		mobAttack = GetComponent<MobAttack>();
	}
}
