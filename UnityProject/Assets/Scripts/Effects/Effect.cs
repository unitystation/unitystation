using System;
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
		if ( !CustomNetworkManager.Instance.isServer )
		{
			return;
		}

		PlayParticleMessage.SendToAll( gameObject, targetVector );
	}
}