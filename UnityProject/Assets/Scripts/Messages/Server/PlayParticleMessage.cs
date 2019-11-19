using System;
using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Tell all clients + server to play particle effect for provided gameObject. Object should have ParticleSystem in hierarchy
/// </summary>
public class PlayParticleMessage : ServerMessage
{
	public static short MessageType = ( short ) MessageTypes.PlayParticleMessage;
	/// <summary>
	/// GameObject containing ParticleSystem
	/// </summary>
	public uint 	ParticleObject;
	public uint 	ParentObject;
	public float 	Angle;
	public bool 	UseAngle;

	///To be run on client
	public override IEnumerator Process()
	{
		if (ParticleObject.Equals(NetId.Invalid)) {
			//Failfast
			Logger.LogWarning("PlayParticle NetId invalid, processing stopped", Category.NetMessage);
			yield break;
		}

		yield return WaitFor(ParticleObject, ParentObject );

		GameObject particleObject = NetworkObjects[0];
		GameObject parentObject = NetworkObjects[1];

		if ( !particleObject.activeInHierarchy )
		{
			Logger.LogFormat("PlayParticle request ignored because gameobject {0} is inactive", Category.NetMessage, particleObject);
			yield break;
		}

		ParticleSystem particleSystem = particleObject.GetComponentInChildren<ParticleSystem>();

		if ( particleSystem == null )
		{
			Logger.LogWarningFormat("ParticleSystem not found for gameobject {0}, PlayParticle request ignored", Category.NetMessage, particleObject);
			yield break;
		}

		//?
		var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
		renderer.enabled = true;

		if ( UseAngle )
		{
			particleSystem.transform.rotation = Quaternion.Euler(0, 0, -Angle+90);
		}

		particleSystem.transform.position = parentObject ? parentObject.WorldPosClient() : particleObject.WorldPosClient();
		//only needs to run on the clients other than the shooter
		particleSystem.Play();
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
		} catch ( Exception ignored ) {}


		PlayParticleMessage msg = new PlayParticleMessage {
			ParticleObject = obj.NetId(),
			ParentObject = topContainer == null ? NetId.Invalid : topContainer.NetId(),
			UseAngle = targetVector != Vector2.zero,
			Angle = Orientation.AngleFromUp( targetVector ),
		};
		msg.SendToAll();
		return msg;
	}

//	public override string ToString()
//	{
//		return " ";
//	}
}
