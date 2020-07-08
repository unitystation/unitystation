using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Interface for processing hit for raycasts
	/// </summary>
	public interface IOnHit
	{
		bool OnHit(RaycastHit2D hit);
	}
}