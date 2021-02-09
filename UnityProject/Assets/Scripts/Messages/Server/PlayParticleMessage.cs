using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Tell all clients + server to play particle effect for provided gameObject. Object should have ParticleSystem in hierarchy
/// </summary>
public class PlayParticleMessage : ServerMessage
{
	/// <summary>
	/// GameObject containing ParticleSystem
	/// </summary>
	public uint 	ParticleObject;
	public uint 	ParentObject;
	public Vector2	TargetVector;

	///To be run on client
	public override void Process()
	{
		if (ParticleObject.Equals(NetId.Invalid)) {
			//Failfast
			Logger.LogWarning("PlayParticle NetId invalid, processing stopped", Category.NetMessage);
			return;
		}

		LoadMultipleObjects(new uint[] {ParticleObject, ParentObject});

		GameObject particleObject = NetworkObjects[0];
		GameObject parentObject = NetworkObjects[1];

		if ( !particleObject.activeInHierarchy )
		{
			Logger.LogFormat("PlayParticle request ignored because gameobject {0} is inactive", Category.NetMessage, particleObject);
			return;
		}


		ParticleSystem particleSystem = particleObject.GetComponentInChildren<ParticleSystem>();

		var reclaimer = particleObject.GetComponent<ParentReclaimer>();

		if (particleSystem == null && reclaimer != null)
		{ //if it's already parented to something else
			reclaimer.ReclaimNow();
			particleSystem = particleObject.GetComponentInChildren<ParticleSystem>();
		}

		if ( particleSystem == null )
		{
			Logger.LogWarningFormat("ParticleSystem not found for gameobject {0}, PlayParticle request ignored", Category.NetMessage, particleObject);
			return;
		}

		var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
		renderer.enabled = true;

		if ( TargetVector != Vector2.zero)
		{
			var angle = Orientation.AngleFromUp(TargetVector);
			particleSystem.transform.rotation = Quaternion.Euler(0, 0, -angle+90);
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

		particleSystem.transform.localPosition = Vector3.zero;

		var customEffectBehaviour = particleSystem.GetComponent<CustomEffectBehaviour>();
		if (customEffectBehaviour)
		{
			customEffectBehaviour.RunEffect(TargetVector);
		}
		else
		{
			//only needs to run on the clients other than the shooter
			particleSystem.Play();
		}
	}

	/// <summary>
	/// Tell all clients + server to play particle effect for provided gameObject
	/// </summary>
	public static PlayParticleMessage SendToAll(GameObject obj, Vector2 targetVector)
	{
		GameObject topContainer = null;
		try
		{
			topContainer = obj.GetComponent<PushPull>().TopContainer.gameObject;
		}
		catch (Exception ignored)
		{
			Debug.Log($"PlayParticleMessage threw an exception {ignored} which has been ignored.");
		}


		PlayParticleMessage msg = new PlayParticleMessage {
			ParticleObject = obj.NetId(),
			ParentObject = topContainer == null ? NetId.Invalid : topContainer.NetId(),
			TargetVector = targetVector,
		};
		msg.SendToAll();
		return msg;
	}

}

/// <summary>
/// Sets provided component to certain parent after a delay
/// </summary>
public class ParentReclaimer : MonoBehaviour
{
	private Coroutine handle;
	private Component lastComponent;
	private Transform lastParent;

	private IEnumerator ReclaimParent(float delaySec, Component componentToParent, Transform parent)
	{
		lastComponent = componentToParent;
		lastParent = parent;
		yield return WaitFor.Seconds(delaySec);
		ReclaimNow();
	}

	/// <summary>
	/// Starts a coroutine to set proper parent for component in question after a delay
	/// </summary>
	/// <param name="delaySec"></param>
	/// <param name="componentToParent"></param>
	/// <param name="parent"></param>
	public void ReclaimWithDelay(float delaySec, Component componentToParent, Transform parent)
	{
		this.RestartCoroutine( ReclaimParent(delaySec, componentToParent, parent), ref handle );
	}

	/// <summary>
	/// Stops coroutine and sets parent immediately
	/// </summary>
	public void ReclaimNow()
	{
		this.TryStopCoroutine(ref handle);
		if (lastComponent == null || lastParent == null)
		{
			return;
		}
		lastComponent.transform.SetParent(lastParent, false);
		lastComponent = null;
		lastParent = null;
	}
}