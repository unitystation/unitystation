using UnityEngine;
using UnityEngine.Serialization;

namespace US13.Effects
{
	public class CustomEffectBehaviour : MonoBehaviour
	{
		[FormerlySerializedAs("particleSystem")]
		public ParticleSystem ParticleSystem;

		public virtual void RunEffect(Vector2 target)
		{

		}
	}
}
