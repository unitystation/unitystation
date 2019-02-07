using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the collision logic for BulletBehavior, responding to the rigidbody collision events and passing them up
///  to BulletBehavior.
///
/// Has to be separate from BulletBehavior because BulletBehavior exists on the parent gameobject, so is unable
/// to respond to collision events on this gameobject.
/// </summary>
public class BulletColliderBehavior : MonoBehaviour
{
	/// <summary>
	/// Cached bulletbehavior in the parent
	/// </summary>
	private BulletBehaviour parentBulletBehavior;

	private void Awake()
	{
		parentBulletBehavior = GetComponentInParent<BulletBehaviour>();
	}

	private void OnCollisionEnter2D(Collision2D other)
	{
		parentBulletBehavior.HandleCollisionEnter2D(other);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		parentBulletBehavior.HandleTriggerEnter2D(other);
	}
}
