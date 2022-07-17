using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.EventTrigger;

namespace Objects
{
	/// <summary>
	/// USE BASE CLASS EnterTileBase NOT THIS INTERFACE FOR OBJECTS
	/// Allows to trigger an event when a player enter the tile of this object.
	/// Does trigger for ghosts, so check in OnStep
	/// ServerSide Only
	/// </summary>
	public interface IPlayerEntersTile
	{
		/// <summary>
		/// Check whether the player should be allowed to trigger the OnPlayerStep logic
		/// </summary>
		/// <param name="playerScript">Playerscript of the player stepping</param>
		/// <returns>False to block OnPlayerStep, true to run OnPlayerStep</returns>
		public abstract bool WillAffectPlayer(PlayerScript playerScript);

		/// <summary>
		/// Action when player steps on tile
		/// </summary>
		/// <param name="playerScript">Playerscript of the player stepping</param>
		public abstract void OnPlayerStep(PlayerScript playerScript);
	}

	/// <summary>
	/// USE BASE CLASS EnterTileBase NOT THIS INTERFACE FOR OBJECTS
	/// Allows to trigger an event when a object (includes mobs, objects and items) enter the tile of this object.
	/// ServerSide Only
	/// </summary>
	public interface IObjectEntersTile
	{
		/// <summary>
		/// Check whether this mobs, object or item should be allowed to trigger the OnObjectEnter logic
		/// </summary>
		/// <returns>False to block OnObjectEnter, true to run OnObjectEnter</returns>
		public abstract bool WillAffectObject(GameObject eventData);

		/// <summary>
		/// Action when a mob, object or item moves to this tile
		/// </summary>
		public abstract void OnObjectEnter(GameObject eventData);
	}
}
