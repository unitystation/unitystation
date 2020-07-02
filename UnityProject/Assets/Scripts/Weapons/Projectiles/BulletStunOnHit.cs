using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stuns players hit by the bullet
/// </summary>
public class BulletStunOnHit : BulletHitTrigger
{
	/// <summary>
	/// How long the player hit by this will be stunned
	/// </summary>
	[SerializeField]
	private float stunTime = 4.0f;
	[SerializeField]
	private bool dropItem = true;

	public override void BulletHitInteract(GameObject target)
	{
		target?.GetComponent<RegisterPlayer>()?.ServerStun(stunTime, dropItem);
	}
}
