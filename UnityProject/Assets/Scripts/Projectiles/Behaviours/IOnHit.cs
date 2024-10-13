namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Interface for processing hit for raycasts. If false is returned, the hit will not count / be despawned.
	/// </summary>
	public interface IOnHit
	{
		bool OnHit(MatrixManager.CustomPhysicsHit hit);
	}
}
