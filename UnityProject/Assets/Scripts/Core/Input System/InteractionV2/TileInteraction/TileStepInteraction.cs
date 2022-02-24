using Objects;
using UnityEngine;

namespace Systems.Interaction
{
	/// <summary>
	/// Tiles are not prefabs, but we still want to be able to associate interaction logic with them.
	/// This abstract base scriptable object allows tiles to define their interaction logic by referencing
	/// subclasses of this class.
	/// </summary>
	public abstract class TileStepInteraction : ScriptableObject, IPlayerEntersTile, IObjectEntersTile
	{
		//Player enter tile interaction//
		public virtual bool WillAffectPlayer(PlayerScript playerScript)
		{
			return false;
		}
		public virtual void OnPlayerStep(PlayerScript playerScript) { }

		//Object, mob, item enter tile interaction//
		public virtual bool WillAffectObject(GameObject eventData)
		{
			return false;
		}
		public virtual void OnObjectEnter(GameObject eventData) { }
	}
}