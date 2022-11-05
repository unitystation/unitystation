using UnityEngine;

namespace Core.Characters
{
	public abstract class CharacterAttributeBehavior : MonoBehaviour
	{
		[SerializeField, Tooltip("Spawn this prefab as soon as it's associated attribute gets added to a character?")]
		private bool spawn = false;

		public bool Spawn => spawn;
		[SerializeField] private bool destroyWhenDone = false;

		/// <summary>
		/// What does this attribute do as soon as it's added to the player?
		/// </summary>
		public abstract void Run(GameObject characterBody);

		public virtual void Cleanup()
		{
			if(destroyWhenDone) Destroy(gameObject);
		}
	}
}