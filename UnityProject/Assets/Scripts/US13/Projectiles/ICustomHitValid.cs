using US13.Managers.MatrixManager;

namespace US13.Projectiles
{
	public interface ICustomHitValid
	{
		public bool IsHitValid(MatrixManager.CustomPhysicsHit hit);
	}
}