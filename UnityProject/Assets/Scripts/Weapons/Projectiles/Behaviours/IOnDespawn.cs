namespace Weapons.Projectiles.Behaviours
{
	public interface IOnDespawn
	{
		/// <summary>
		/// Interface for notifying components that
		/// game object is about to be despawned
		/// </summary>
		void OnDespawn();
	}
}