using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger for when the bullet hits a living target
/// </summary>
[RequireComponent(typeof(BulletBehaviour))]
public abstract class BulletHitTrigger : MonoBehaviour
{
	/// <summary>
	/// Called by BulletBehaviour.cs when the bullet hits a living target
	/// </summary>
	/// <param name="target">The target hit</param>
	public abstract	void BulletHitInteract(GameObject target);
}
