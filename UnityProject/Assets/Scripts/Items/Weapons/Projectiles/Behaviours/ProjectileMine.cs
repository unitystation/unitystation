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

		[SerializeField] private int numberOfTilesAffected = 1;

		public virtual bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{

			var tile = interactableTiles.InteractableLayerTileAt(worldPosition, true);
			if (tile is BasicTile basicTile)
			{

				if (projectileHardness < basicTile.MiningHardness)
				{
					SoundManager.PlayNetworkedAtPos(projectileMineFail, gameObject.AssumedWorldPosServer());
					Chat.AddActionMsgToChat(gameObject, $"The projectile pings off the surface, leaving hardly a scratch.");
					return false;
				}
			}

			if (numberOfTilesAffected <= 1) return interactableTiles.TryMine(worldPosition);

			for (int i = 0; i < numberOfTilesAffected; i++)
			{
				Vector3[] positionsToDamage = new Vector3[4]
				{
					new Vector3(worldPosition.x + i, worldPosition.y),
					new Vector3(worldPosition.x - i, worldPosition.y),
					new Vector3(worldPosition.x, worldPosition.y + i),
					new Vector3(worldPosition.x, worldPosition.y - i)
				};
				foreach (var position in positionsToDamage)
				{
					interactableTiles.TryMine(position);
				}
			}

			return interactableTiles.TryMine(worldPosition);
		}

	}

}