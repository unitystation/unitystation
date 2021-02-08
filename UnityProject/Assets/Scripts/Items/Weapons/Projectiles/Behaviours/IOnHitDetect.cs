using ScriptableObjects.Gun;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Interface for processing hit on objects with this interface, allows the object to react to what it has been hit by.
	/// Eg field generator charging when hit by laser from emitter
	/// </summary>
	public interface IOnHitDetect
	{
		void OnHitDetect(DamageData damageData);
	}
}
