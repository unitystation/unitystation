using UnityEngine;

namespace Core.Characters
{
	public abstract class CharacterAttributeBehavior : MonoBehaviour
	{
		public bool Spawn = false;
		private bool destroyWhenDone = false;

		/// <summary>
		/// What does this attribute do as soon as it's added to the player?
		/// </summary>
		public abstract void Run(PlayerScript script);

		public virtual void Cleanup()
		{
			if(destroyWhenDone) Destroy(gameObject);
		}
	}
}