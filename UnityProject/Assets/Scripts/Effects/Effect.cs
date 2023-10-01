using System;
using Logs;
using Messages.Server;
using Mirror;
using UnityEngine;

/// <summary>
/// Collection of static methods for convenient use
/// </summary>
public class Effect
{
	/// <summary>
	///  Trigger particle system for provided object, positioned at its top parent.
	///	 Parent is resolved on serverside, Position is calculated on clientside.
	/// </summary>
	/// <param name="gameObject">Object with ParticleSystem in hierarchy</param>
	public static void PlayParticle( GameObject gameObject )
	{
		PlayParticleDirectional( gameObject, Vector2.zero );
	}

	/// <summary>
	///  Trigger particle system for provided object, positioned at its top parent.
	///	 Parent is resolved on serverside, Position is calculated on clientside.
	/// </summary>
	/// <param name="gameObject">Object with ParticleSystem in hierarchy</param>
	/// <param name="targetVector">AimApply interaction TargetVector (relative to gameObject)</param>
	public static void PlayParticleDirectional( GameObject gameObject, Vector2 targetVector )
	{
		if ( !CustomNetworkManager.Instance._isServer )
		{
			return;
		}

		PlayParticleMessage.SendToAll( gameObject, targetVector );
	}

	[Client]
	public static void ClientPlayParticle(GameObject particleObject, GameObject parentObject, Vector2 targetVector, bool resetToZero = true)
	{
		if (particleObject == null)
		{
			Loggy.LogError("Failed to load particle in PlayParticleMessage", Category.Particles);
			return;
		}

		if (particleObject.activeInHierarchy == false)
		{
			Loggy.LogFormat("PlayParticle request ignored because gameobject {0} is inactive", Category.Particles,
				particleObject);
			return;
		}


		ParticleSystem particleSystem = particleObject.GetComponentInChildren<ParticleSystem>();

		var reclaimer = particleObject.GetComponent<ParentReclaimer>();

		if (particleSystem == null && reclaimer != null)
		{
			//if it's already parented to something else
			reclaimer.ReclaimNow();
			particleSystem = particleObject.GetComponentInChildren<ParticleSystem>();
		}

		if (particleSystem == null)
		{
			Loggy.LogWarningFormat("ParticleSystem not found for gameobject {0}, PlayParticle request ignored",
				Category.Particles, particleObject);
			return;
		}

		var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
		renderer.enabled = true;

		if (targetVector != Vector2.zero)
		{
			var angle = Orientation.AngleFromUp(targetVector);
			particleSystem.transform.rotation = Quaternion.Euler(0, 0, -angle + 90);
		}

		if (parentObject != null)
		{
			//temporary change of parent, but setting it back after playback ends!
			if (reclaimer == null)
			{
				reclaimer = particleObject.AddComponent<ParentReclaimer>();
			}

			reclaimer.ReclaimWithDelay(particleSystem.main.duration, particleSystem, particleObject.transform);

			particleSystem.transform.SetParent(parentObject.transform, false);
		}

		if (resetToZero)
		{
			particleSystem.transform.localPosition = Vector3.zero;
		}

		var customEffectBehaviour = particleSystem.GetComponent<CustomEffectBehaviour>();
		if (customEffectBehaviour)
		{
			customEffectBehaviour.RunEffect(targetVector);
		}
		else
		{
			//only needs to run on the clients other than the shooter
			particleSystem.Play();
		}
	}
}