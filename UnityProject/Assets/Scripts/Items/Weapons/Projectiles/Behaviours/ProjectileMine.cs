using UnityEngine;
using Tiles;
using AddressableReferences;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Mines mineable walls on collision if hardness of the projectile is equal to or exceeds the tile's hardness
	/// </summary>

	public class ProjectileMine : MonoBehaviour, IOnHitInteractTile
	{

		[Range(1, 10)]
		[Tooltip("what degree of hardness this projectile can overcome. Higher means the projectile can mine more types of things.")]
		[SerializeField] private int projectileHardness = 5;

		[Tooltip("The sound made when this projectile fails to break a tile due to insufficient hardness.")]
		public AddressableAudioSource projectileMineFail;

		public virtual bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{

			var tile = interactableTiles.InteractableLayerTileAt(worldPosition, true);
			if (tile is BasicTile basicTile)
			{

				if (projectileHardness < basicTile.MiningHardness)
				{
					SoundManager.PlayNetworkedAtPos(projectileMineFail, gameObject.AssumedWorldPosServer());
					Chat.AddLocalMsgToChat($"The projectile pings off the surface, leaving hardly a scratch.", gameObject);
					return false;
				}
				
				
			}
			return interactableTiles.TryMine(worldPosition);
		}

	}

}