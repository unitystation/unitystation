using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for artifact effect
/// </summary>
public class ArtifactEffect : ScriptableObject
{
	/// <summary>
	/// Called when artifact was touched by player in any intent.
	/// Usually effect will be applied to this player.
	/// </summary>
	/// <param name="artifact">Artifact instance</param>
	/// <param name="touchSource">Player GameObject that touched artifact</param>
	public virtual void DoEffectTouch(Artifact artifact, GameObject touchSource)
	{

	}

	/// <summary>
	/// Called if artifact just emits aura.
	/// Usually effect will be applied to all actors in radius.
	/// </summary>
	/// <param name="artifact">Artifact instance</param>
	public virtual void DoEffectAura(Artifact artifact)
	{

	}

	/// <summary>
	/// Called if artifact was activated by some trigger.
	/// It can be heat, emiters beam or something else.
	/// </summary>
	/// <param name="artifact">Artifact instance</param>
	/// <param name="pulseSource">Object that activated artifact. Might be null.</param>
	public virtual void DoEffectPulse(Artifact artifact, GameObject pulseSource)
	{

	}
}
