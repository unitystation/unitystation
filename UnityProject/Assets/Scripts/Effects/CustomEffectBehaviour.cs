using UnityEngine;
using UnityEngine.Serialization;

public class CustomEffectBehaviour : MonoBehaviour
{
	[FormerlySerializedAs("particleSystem")]
	public ParticleSystem ParticleSystem;

	public virtual void RunEffect(Vector2 target)
	{

	}
}
